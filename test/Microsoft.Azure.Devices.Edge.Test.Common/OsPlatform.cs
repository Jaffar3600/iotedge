// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Test.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Edge.Test.Common.Certs;
    using Microsoft.Azure.Devices.Edge.Util;
    using Serilog;

    public class OsPlatform
    {
        public static readonly IOsPlatform Current = IsWindows() ? new Windows.OsPlatform() as IOsPlatform : new Linux.OsPlatform();

        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        protected EdgeCertificates GetEdgeQuickstartCertificates(string basePath) =>
            new EdgeCertificates(
                Directory.GetFiles(Path.Combine(basePath, "certs"), "device_ca_alias*.pem")[0],
                Directory.GetFiles(Path.Combine(basePath, "cert_keys"), "device_ca_alias*.pem")[0],
                Directory.GetFiles(Path.Combine(basePath, "certs"), "edge_owner_ca*.pem")[0]);

        protected async Task InstallRootCertificateAsync(
            string basePath,
            (string name, string args) command,
            CancellationToken token)
        {
            string[] output = await Process.RunAsync(command.name, command.args, token);
            Log.Verbose(string.Join("\n", output));

            var files = new[]
            {
                "certs/azure-iot-test-only.root.ca.cert.pem",
                "private/azure-iot-test-only.root.ca.key.pem"
            };

            CheckFiles(files, basePath);
        }

        protected void InstallTrustedCertificates(IEnumerable<X509Certificate2> certs, StoreName storeName)
        {
            using (var store = new X509Store(storeName, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                foreach (var cert in certs)
                {
                    store.Add(cert);
                }
            }
        }

        protected async Task RunScriptAsync(
            (string name, string args) command,
            CancellationToken token)
        {
            Log.Verbose("Executed: " + command.name + ' ' + command.args);
            string[] output = await Process.RunAsync(command.name, command.args, token);
            Log.Verbose(string.Join("\n", output));
        }

        static void CheckFiles(IEnumerable<string> paths, string basePath) => NormalizeFiles(paths, basePath);

        public static string[] NormalizeFiles(IEnumerable<string> paths, string basePath)
        {
            return paths.Select(
                path =>
                {
                    var file = new FileInfo(Path.Combine(basePath, path));
                    Preconditions.CheckArgument(file.Exists, $"File Not Found: {file.FullName}");
                    return file.FullName;
                }).ToArray();
        }
    }
}
