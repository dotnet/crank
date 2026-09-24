// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.Crank.Agent;

internal sealed class RelayServer : IServer
{
    private readonly object _gate = new();
    private readonly RelayServerOptions _options;
    private readonly IRelayListenerFactory _factory;
    private readonly ILogger<RelayServer> _logger;
    private readonly CancellationTokenSource _abort = new();
    private readonly HashSet<Task> _requests = new();
    private readonly HashSet<Task> _applications = new();
    private readonly ServerAddressesFeature _addresses = new();
    private CancellationTokenSource _opening;
    private IRelayListener _listener;
    private Task _start;
    private Task _stop;
    private Task _cancellation;
    private Task _openTask = Task.CompletedTask;
    private Task _closeTask;
    private Task _openingCancellation = Task.CompletedTask;
    private Task _openingCleanup;
    private bool _accepting;
    private bool _stopping;

    public RelayServer(RelayServerOptions options, IRelayListenerFactory factory, ILogger<RelayServer> logger)
    {
        options.ValidateTimeouts();
        _options = options;
        _factory = factory;
        _logger = logger;
        Features = new FeatureCollection();
        Features.Set<IServerAddressesFeature>(_addresses);
    }

    public IFeatureCollection Features { get; }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        lock (_gate)
        {
            if (_stopping)
            {
                throw new InvalidOperationException("A stopped Relay server cannot be restarted.");
            }

            return _start ??= StartCoreAsync(application, cancellationToken);
        }
    }

    private async Task StartCoreAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        // Yield before touching state so StartAsync publishes its task before StopAsync can observe it.
        await Task.Yield();
        CancellationTokenSource opening = null;
        var openingToken = cancellationToken;
        try
        {
            lock (_gate)
            {
                opening = _opening = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                opening.CancelAfter(_options.OpenTimeout);
                openingToken = opening.Token;
                if (_stopping)
                {
                    throw new OperationCanceledException("Relay startup was stopped.", openingToken);
                }

                openingToken.ThrowIfCancellationRequested();
                _listener = _factory.Create(_options);
                _listener.RequestHandler = exchange => Dispatch(exchange, application);
                var listener = _listener;
                // Token callbacks in the SDK can ignore cancellation or block synchronously.
                _openTask = Task.Run(() => listener.OpenAsync(openingToken));
            }

            await _openTask.WaitAsync(openingToken).ConfigureAwait(false);
            lock (_gate)
            {
                openingToken.ThrowIfCancellationRequested();
                if (_stopping)
                {
                    throw new OperationCanceledException("Relay startup was stopped.", openingToken);
                }
                _accepting = true;
                // WebHost may have inserted --url into this feature. Relay-only must never advertise it.
                _addresses.Addresses.Clear();
                _addresses.Addresses.Add(_options.PublicAddress.AbsoluteUri);
            }
        }
        catch
        {
            lock (_gate)
            {
                _accepting = false;
                _stopping = true;
            }

            _ = BeginCloseListener();
            if (!openingToken.IsCancellationRequested)
            {
                await CloseListenerAsync().ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("Relay startup was cancelled; SDK open and close operations will be observed until they finish.");
            }
            throw;
        }
        finally
        {
            lock (_gate)
            {
                _opening = null;
                if (opening != null)
                {
                    _openingCleanup = FinishOpeningAsync(opening, _openTask,
                        _closeTask ?? Task.CompletedTask, _openingCancellation);
                }
            }
        }
    }

    private async Task FinishOpeningAsync(CancellationTokenSource opening, Task open, Task close, Task cancellation)
    {
        try
        {
            await open.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "The SDK Relay open operation failed.");
        }

        // Closing the SDK can itself be waiting for open. Neither operation may use a
        // disposed token source when it eventually resumes after the owned wait expires.
        await close.ConfigureAwait(false);
        await cancellation.ConfigureAwait(false);
        opening.Dispose();
    }

    private void Dispatch<TContext>(RelayHttpExchange exchange, IHttpApplication<TContext> application)
        where TContext : notnull
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool accepted;
        lock (_gate)
        {
            accepted = _accepting;
            _requests.Add(completion.Task);
            if (accepted)
            {
                _applications.Add(completion.Task);
            }
        }

        // The SDK invokes an Action. Register work before scheduling it, including synchronous completions
        // and rejection responses. A slow upload must not block dispatch of job keepalives.
        _ = Task.Run(async () =>
        {
            try
            {
                if (accepted)
                {
                    await new RelayHttpContext(exchange, _options.PublicAddress, _abort.Token, _logger)
                        .ProcessAsync(application).ConfigureAwait(false);
                }
                else
                {
                    await RelayHttpContext.RejectAsync(exchange, _logger).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected Relay request processing failure.");
            }
            finally
            {
                lock (_gate)
                {
                    _requests.Remove(completion.Task);
                    _applications.Remove(completion.Task);
                    completion.SetResult();
                }
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_stop != null)
            {
                return _stop;
            }
            _stopping = true;
            _accepting = false;
            if (_opening != null)
            {
                _openingCancellation = ObserveCancellationAsync(_opening.CancelAsync(), "startup");
            }
            return _stop = StopCoreAsync(cancellationToken);
        }
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        if (_start != null)
        {
            try
            {
                await _start.WaitAsync(_options.CloseTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is OperationCanceledException or TimeoutException)
            {
                _logger.LogWarning("Relay shutdown will not wait further for startup.");
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Relay startup failed; finishing shutdown.");
            }
        }

        Task applications;
        lock (_gate)
        {
            applications = Task.WhenAll(_applications.ToArray());
        }

        try
        {
            await applications.WaitAsync(_options.DrainTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException or TimeoutException)
        {
            _logger.LogWarning("Relay request drain deadline expired; cancelling active requests.");
            BeginRequestCancellation();
        }

        // The SDK does not drain application HTTP work. Keep it open until the drain above finishes.
        await CloseListenerAsync().ConfigureAwait(false);
        BeginRequestCancellation();
        if (_openingCleanup is { IsCompleted: false })
        {
            _logger.LogWarning("SDK Relay startup cleanup is still pending after shutdown.");
        }

        Task remaining;
        lock (_gate)
        {
            remaining = Task.WhenAll(_requests.Append(_cancellation));
        }

        try
        {
            await remaining.WaitAsync(_options.CancellationGracePeriod).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Relay requests are still completing after shutdown. The SDK response close API is not cancellable.");
        }

        // Do not dispose the token source underneath an application that ignored cancellation.
        _ = remaining.ContinueWith(_ => _abort.Dispose(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void BeginRequestCancellation()
    {
        // User cancellation callbacks must not block the host's stopping thread.
        _cancellation ??= ObserveCancellationAsync(_abort.CancelAsync());
    }

    private async Task ObserveCancellationAsync(Task cancellation, string operation = "request")
    {
        try
        {
            await cancellation.ConfigureAwait(false);
        }
        catch (AggregateException exception)
        {
            _logger.LogError(exception, "Relay {Operation} cancellation callback failed.", operation);
        }
    }

    private async Task CloseListenerAsync()
    {
        var close = BeginCloseListener();
        try
        {
            await close.WaitAsync(_options.CloseTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Relay listener close exceeded its deadline; observing its eventual completion in the background.");
        }
    }

    private Task BeginCloseListener()
    {
        lock (_gate)
        {
            if (_listener == null)
            {
                return Task.CompletedTask;
            }

            var listener = _listener;
            return _closeTask ??= Task.Run(() => FinishCloseAsync(listener));
        }
    }

    private async Task FinishCloseAsync(IRelayListener listener)
    {
        using var timeout = new CancellationTokenSource(_options.CloseTimeout);
        try
        {
            await listener.CloseAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to close the Relay listener.");
        }
    }

    public void Dispose() => StopAsync(CancellationToken.None).GetAwaiter().GetResult();
}
