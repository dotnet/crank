using System.Net.Http.Json;

Console.WriteLine("Application started.");

var agentUrl = args[0];

Console.WriteLine($"BaseAddress: {agentUrl}");

using var httpClient = new HttpClient();

var response = await httpClient.PostAsync($"{agentUrl}/metadata?name=cpu&aggregate=max&reduce=max&format=n0&longDescription=Long%20description&shortDescription=Short%20description", new StringContent(""));

Console.WriteLine(response);

response = await httpClient.PostAsync($"{agentUrl}/measurement?name=cpu&timestamp=2024-02-23T14:00:00Z&value=123.456", new StringContent(""));

Console.WriteLine(response);

var statistics = new
{
    Metadata = new[]
    {
        new
        {
            Name = "metadata1",
            Aggregate = "Max",
            Reduce = "Max",
            Format = "n0",
            LongDescription = "Long description 1",
            ShortDescription = "Short description 1"
        },
        new
        {
            Name = "metadata2",
            Aggregate = "Min",
            Reduce = "Min",
            Format = "n2",
            LongDescription = "Long description 2",
            ShortDescription = "Short description 2"
        }
    },

    Measurements = new[]
    {
        new
        {
            Name = "metadata1",
            Timestamp = "2024-02-23T14:00:00Z",
            Value = 123.456M
        },
        new
        {
            Name = "metadata2",
            Timestamp = "2024-02-23T14:00:00Z",
            Value = 123.456M
        }
    }
};


response = await httpClient.PostAsJsonAsync($"{agentUrl}/statistics", statistics);

Console.WriteLine(response);
