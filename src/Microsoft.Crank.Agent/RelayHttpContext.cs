// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Crank.Agent;

internal sealed class RelayHttpContext : IHttpResponseFeature, IHttpResponseBodyFeature,
    IHttpRequestLifetimeFeature, IHttpMaxRequestBodySizeFeature, IHttpRequestBodyDetectionFeature
{
    internal const long DefaultMaxRequestBodySize = 10L * 1024 * 1024 * 1024;

    private readonly RelayHttpExchange _exchange;
    private readonly Uri _publicAddress;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _aborted;
    private readonly CancellationTokenRegistration _shutdownRegistration;
    private readonly List<(Func<object, Task> Callback, object State)> _starting = new();
    private readonly List<(Func<object, Task> Callback, object State)> _completedCallbacks = new();
    private readonly AsyncLocal<bool> _insideStartingCallback = new();
    private CancellationTokenSource _applicationAborted;
    private HeaderDictionary _headers = new();
    private RequestStream _input;
    private ResponseStream _output;
    private PipeWriter _writer;
    private Task _startTask;
    private Task _completeTask;
    private Task<Exception> _closeTask;
    private bool _completed;
    private bool _finished;
    private bool _completingCallbacks;
    private int _statusCode = StatusCodes.Status200OK;
    private string _reasonPhrase;
    private long? _maxRequestBodySize;

    public RelayHttpContext(RelayHttpExchange exchange, Uri publicAddress,
        CancellationToken cancellationToken, ILogger logger,
        long? maxRequestBodySize = DefaultMaxRequestBodySize)
    {
        ArgumentNullException.ThrowIfNull(exchange);
        ArgumentNullException.ThrowIfNull(publicAddress);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRequestBodySize ?? 0);
        _exchange = exchange;
        _publicAddress = publicAddress;
        _logger = logger;
        _maxRequestBodySize = maxRequestBodySize;
        _aborted = new CancellationTokenSource();
        // Observe exceptions thrown by application cancellation callbacks even when
        // cancellation originates from server shutdown rather than an I/O operation.
        _shutdownRegistration = cancellationToken.Register(static state => ((RelayHttpContext)state).Abort(), this);
    }

    public async Task ProcessAsync<TContext>(IHttpApplication<TContext> application)
    {
        TContext context = default;
        var created = false;
        Exception error = null;
        try
        {
            var features = CreateFeatures();
            context = application.CreateContext(features);
            created = true;
            await application.ProcessRequestAsync(context).ConfigureAwait(false);
            await CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            error = exception;
            _logger.LogError(exception, "Relay HTTP request failed.");
            if (!HasStarted)
            {
                try
                {
                    // Like Kestrel, don't let OnStarting callbacks turn an unhandled
                    // application failure into a successful response.
                    _starting.Clear();
                    _headers = new HeaderDictionary();
                    _statusCode = exception is BadHttpRequestException badRequest
                        ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;
                    _reasonPhrase = null;
                    Commit();
                }
                catch (Exception commitError)
                {
                    _logger.LogError(commitError, "Committing the Relay HTTP error response failed.");
                }
            }
        }
        finally
        {
            _finished = true;
            // The SDK close operation has no cancellation overload. Await it rather than
            // abandon its task or imply that cancellation imposes a hard close deadline.
            var closeError = await CloseResponseAsync().ConfigureAwait(false);
            if (closeError != null)
            {
                Abort();
            }
            error ??= closeError;
            _completingCallbacks = true;
            await InvokeCallbacksAsync(_completedCallbacks, "OnCompleted").ConfigureAwait(false);
            if (created)
            {
                try
                {
                    application.DisposeContext(context, error);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Disposing the Relay HTTP application context failed.");
                }
            }

            try
            {
                if (_input != null)
                {
                    _input.Dispose();
                }
                else
                {
                    _exchange.InputStream.Dispose();
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Disposing the Relay HTTP request stream failed.");
            }

            if (_writer != null && !_completed)
            {
                try
                {
                    // Discard pending bytes after a failure; never flush them after close.
                    await _writer.CompleteAsync(error).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Disposing the Relay HTTP response writer failed.");
                }
            }

            _shutdownRegistration.Dispose();
            _applicationAborted?.Dispose();
            _aborted.Dispose();
        }
    }

    public static async Task RejectAsync(RelayHttpExchange exchange, ILogger logger)
    {
        try
        {
            exchange.Commit(StatusCodes.Status503ServiceUnavailable,
                ReasonPhrases.GetReasonPhrase(StatusCodes.Status503ServiceUnavailable), new HeaderDictionary());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Committing the rejected Relay HTTP response failed.");
        }
        finally
        {
            await CloseAsync(exchange, logger).ConfigureAwait(false);
            try
            {
                exchange.InputStream.Dispose();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Disposing the rejected Relay HTTP request stream failed.");
            }
        }
    }

    private static async Task<Exception> CloseAsync(RelayHttpExchange exchange, ILogger logger)
    {
        try
        {
            await exchange.CloseAsync().ConfigureAwait(false);
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Closing the Relay HTTP response failed.");
            return exception;
        }
    }

    private Task<Exception> CloseResponseAsync() => _closeTask ??= CloseAsync(_exchange, _logger);

    private IFeatureCollection CreateFeatures()
    {
        _input = new RequestStream(this, _exchange.InputStream);
        _output = new ResponseStream(this);
        var url = _exchange.Url;
        var path = PathString.FromUriComponent(url.AbsolutePath);
        var prefix = PathString.FromUriComponent(_publicAddress.AbsolutePath.TrimEnd('/'));
        var pathBase = PathString.Empty;
        if (prefix.HasValue && path.StartsWithSegments(prefix, StringComparison.Ordinal, out var remaining))
        {
            pathBase = prefix;
            path = remaining;
        }

        var requestHeaders = new HeaderDictionary();
        foreach (var name in _exchange.Headers.AllKeys)
        {
            requestHeaders[name] = new StringValues(_exchange.Headers.GetValues(name));
        }

        requestHeaders["Host"] = url.Authority;
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature
        {
            Protocol = "HTTP/1.1",
            Scheme = _publicAddress.Scheme,
            Method = _exchange.Method,
            PathBase = pathBase.Value ?? "",
            Path = path.Value ?? "",
            QueryString = url.Query,
            RawTarget = url.PathAndQuery,
            Headers = requestHeaders,
            Body = _input
        });
        features.Set<IHttpResponseFeature>(this);
        features.Set<IHttpResponseBodyFeature>(this);
        features.Set<IHttpRequestLifetimeFeature>(this);
        features.Set<IHttpMaxRequestBodySizeFeature>(this);
        features.Set<IHttpRequestBodyDetectionFeature>(this);
        features.Set<IHttpConnectionFeature>(new HttpConnectionFeature
        {
            RemoteIpAddress = _exchange.RemoteEndPoint?.Address,
            RemotePort = _exchange.RemoteEndPoint?.Port ?? 0
        });
        return features;
    }

    public int StatusCode
    {
        get => _statusCode;
        set { ThrowIfStarted(); _statusCode = value; }
    }

    public string ReasonPhrase
    {
        get => _reasonPhrase;
        set { ThrowIfStarted(); _reasonPhrase = value; }
    }

    public IHeaderDictionary Headers
    {
        get => _headers;
        set
        {
            ThrowIfStarted();
            var headers = new HeaderDictionary();
            foreach (var header in value)
            {
                headers.Add(header);
            }
            _headers = headers;
        }
    }

    // Modern HttpResponse.Body replacement is managed by DefaultHttpResponse using a
    // StreamResponseBodyFeature. The original stream remains the server's output sink.
    public Stream Body { get => _output; set => throw new NotSupportedException(); }
    public bool HasStarted { get; private set; }
    public Stream Stream => _output;
    public PipeWriter Writer => _writer ??= PipeWriter.Create(_output, new StreamPipeWriterOptions(leaveOpen: true));
    public bool CanHaveBody => _exchange.HasEntityBody;
    public bool IsReadOnly { get; private set; }

    public long? MaxRequestBodySize
    {
        get => _maxRequestBodySize;
        set
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("The request body size limit cannot change after reading starts.");
            }
            ArgumentOutOfRangeException.ThrowIfNegative(value ?? 0);
            _maxRequestBodySize = value;
        }
    }

    public CancellationToken RequestAborted
    {
        get => _applicationAborted?.Token ?? _aborted.Token;
        set
        {
            _applicationAborted?.Dispose();
            _applicationAborted = CancellationTokenSource.CreateLinkedTokenSource(_aborted.Token, value);
        }
    }

    public void Abort()
    {
        try
        {
            _aborted.Cancel();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "A Relay HTTP cancellation callback failed.");
        }
    }

    public void OnStarting(Func<object, Task> callback, object state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfStarted();
        _starting.Add((callback, state));
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (_completingCallbacks)
        {
            throw new InvalidOperationException("Response completion callbacks have already started.");
        }
        _completedCallbacks.Add((callback, state));
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (HasStarted)
        {
            return Task.CompletedTask;
        }

        // A callback can write a body, including after an await. Its nested start must
        // finish the remaining callbacks instead of awaiting the outer start task.
        return _insideStartingCallback.Value
            ? StartCoreAsync(cancellationToken)
            : _startTask ??= StartCoreAsync(cancellationToken);
    }

    private async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        var wasStarting = _insideStartingCallback.Value;
        _insideStartingCallback.Value = true;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestAborted.ThrowIfCancellationRequested();
            var error = await InvokeCallbacksAsync(_starting, "OnStarting", stopOnError: true).ConfigureAwait(false);
            if (error != null)
            {
                throw error;
            }
            if (!HasStarted)
            {
                Commit();
            }
        }
        catch (Exception exception)
        {
            ObserveFailure(exception);
            throw;
        }
        finally
        {
            _insideStartingCallback.Value = wasStarting;
        }
    }

    private void Commit()
    {
        if (_statusCode == StatusCodes.Status204NoContent)
        {
            _headers.Remove("Content-Length");
            _headers.Remove("Transfer-Encoding");
        }
        _exchange.Commit(_statusCode, _reasonPhrase ?? ReasonPhrases.GetReasonPhrase(_statusCode), _headers);
        HasStarted = true;
        _headers.IsReadOnly = true;
    }

    public Task CompleteAsync() => _completeTask ??= CompleteCoreAsync();

    private async Task CompleteCoreAsync()
    {
        await StartAsync().ConfigureAwait(false);
        if (_writer != null)
        {
            await _writer.FlushAsync(RequestAborted).ConfigureAwait(false);
            await _writer.CompleteAsync().ConfigureAwait(false);
        }
        _completed = true;
        var error = await CloseResponseAsync().ConfigureAwait(false);
        if (error != null)
        {
            Abort();
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    public void DisableBuffering()
    {
        // Relay SDK FlushAsync is a no-op. Its small response buffering (until close,
        // about 64 KB, or about two seconds) cannot be disabled through the SDK.
    }

    public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count ?? 0);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(RequestAborted, cancellationToken);
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (offset > file.Length || count > file.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            file.Position = offset;
            var remaining = count ?? file.Length - offset;
            var buffer = new byte[64 * 1024];
            while (remaining > 0)
            {
                var read = await file.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), linked.Token).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }
                await _output.WriteAsync(buffer.AsMemory(0, read), linked.Token).ConfigureAwait(false);
                remaining -= read;
            }
        }
        catch (Exception exception)
        {
            ObserveFailure(exception);
            throw;
        }
    }

    private async Task<Exception> InvokeCallbacksAsync(List<(Func<object, Task> Callback, object State)> callbacks, string name,
        bool stopOnError = false)
    {
        List<Exception> errors = null;
        while (callbacks.Count > 0)
        {
            var callback = callbacks[^1];
            callbacks.RemoveAt(callbacks.Count - 1);
            try
            {
                await callback.Callback(callback.State).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Relay HTTP {Callback} callback failed.", name);
                (errors ??= new()).Add(exception);
                if (stopOnError)
                {
                    break;
                }
            }
        }
        return errors == null ? null : new AggregateException(errors);
    }

    private void ThrowIfStarted()
    {
        if (HasStarted)
        {
            throw new InvalidOperationException("The response has already started.");
        }
    }

    private void ObserveFailure(Exception exception)
    {
        _logger.LogError(exception, "Relay HTTP I/O failed or was canceled.");
        Abort();
    }

    private abstract class HttpStream : Stream
    {
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    private sealed class RequestStream(RelayHttpContext owner, Stream inner) : HttpStream
    {
        private long _read;
        private int _disposed;
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override void Flush() { }
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(owner.RequestAborted, cancellationToken);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed != 0, this);
                linked.Token.ThrowIfCancellationRequested();
                if (buffer.IsEmpty)
                {
                    return 0;
                }
                owner.IsReadOnly = true;
                var available = owner.MaxRequestBodySize.HasValue ? owner.MaxRequestBodySize.Value - _read : long.MaxValue;
                // Probe at most one excess byte, so an exactly-at-limit body can still
                // return EOF and unknown-length bodies never need to be buffered.
                if (available < buffer.Length)
                {
                    buffer = buffer[..(int)(Math.Max(0, available) + 1)];
                }
                var read = await inner.ReadAsync(buffer, linked.Token).ConfigureAwait(false);
                if (read > available)
                {
                    throw new BadHttpRequestException("The Relay HTTP request body is too large.", StatusCodes.Status413PayloadTooLarge);
                }
                _read += read;
                return read;
            }
            catch (Exception exception)
            {
                owner.ObserveFailure(exception);
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                try
                {
                    inner.Dispose();
                }
                catch (Exception exception)
                {
                    owner.ObserveFailure(exception);
                    throw;
                }
            }
            base.Dispose(disposing);
        }
    }

    private sealed class ResponseStream(RelayHttpContext owner) : HttpStream
    {
        public override bool CanRead => false;
        public override bool CanWrite => true;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count)
            => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override void Flush() => FlushAsync().GetAwaiter().GetResult();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(owner.RequestAborted, cancellationToken);
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                if (owner._completed || owner._finished)
                {
                    throw new InvalidOperationException("The response is complete.");
                }
                await owner.StartAsync(linked.Token).ConfigureAwait(false);
                if (!HttpMethods.IsHead(owner._exchange.Method)
                    && owner.StatusCode != StatusCodes.Status204NoContent
                    && owner.StatusCode != StatusCodes.Status304NotModified)
                {
                    await owner._exchange.OutputStream.WriteAsync(buffer, linked.Token).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                owner.ObserveFailure(exception);
                throw;
            }
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(owner.RequestAborted, cancellationToken);
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                if (owner._completed || owner._finished)
                {
                    throw new InvalidOperationException("The response is complete.");
                }
                await owner.StartAsync(linked.Token).ConfigureAwait(false);
                await owner._exchange.OutputStream.FlushAsync(linked.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                owner.ObserveFailure(exception);
                throw;
            }
        }
    }
}
