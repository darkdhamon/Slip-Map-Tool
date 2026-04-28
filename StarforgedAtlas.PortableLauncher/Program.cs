using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

ApplicationConfiguration.Initialize();

var launcher = new PortableLauncher();
await launcher.RunAsync(args);
