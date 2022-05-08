using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ZapWord.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var base_address = new Uri(builder.HostEnvironment.BaseAddress);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = base_address });
builder.Services.AddSingleton<IGameState, GameState>(sp => new GameState(new HttpClient { BaseAddress = base_address }));
await builder.Build().RunAsync();
