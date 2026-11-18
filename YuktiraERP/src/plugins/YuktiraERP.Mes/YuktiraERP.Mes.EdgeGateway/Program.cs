using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YuktiraERP.Mes.EdgeGateway;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<DeadbandFilter>();
builder.Services.AddSingleton<EdgeStoreAndForwardBuffer>();
builder.Services.AddSingleton<IProtocolDriver, OpcUaProtocolDriver>();
builder.Services.AddSingleton<IProtocolDriver, ModbusTcpProtocolDriver>();
builder.Services.AddSingleton<IProtocolDriver, SiemensS7ProtocolDriver>();
builder.Services.AddSingleton<IProtocolDriver, MqttSparkplugBProtocolDriver>();
builder.Services.AddHostedService<EdgeGatewayWorker>();

var host = builder.Build();
await host.RunAsync();
