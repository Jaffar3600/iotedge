// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Test.Common.Certs
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.IO;
    using Microsoft.Azure.Devices.Edge.Util;
    using RootCaKeys = System.ValueTuple<string, string, string>;
    using Serilog;

    public class CertificateAuthority
    {
        readonly Option<string> scriptPath;

        public EdgeCertificates Certificates { get; }

        public Certificate genericCertificates { get; }

        public static async Task<CertificateAuthority> CreateAsync(string deviceId, RootCaKeys rootCa, string scriptPath, CancellationToken token)
        {
            (string rootCertificate, string rootPrivateKey, string rootPassword) = rootCa;
            await OsPlatform.Current.InstallRootCertificateAsync(rootCertificate, rootPrivateKey, rootPassword, scriptPath, token);
            EdgeCertificates certs = await OsPlatform.Current.GenerateEdgeCertificatesAsync(deviceId, scriptPath, token);
            return new CertificateAuthority(certs, scriptPath);
        }

        public static async Task<CertificateAuthority> CreateDpsX509CaAsync(string deviceId, RootCaKeys rootCa, string scriptPath, CancellationToken token)
        {
            if(!File.Exists(Path.Combine(scriptPath, "certs", "azure-iot-test-only.root.ca.cert.pem")))
            {
                // [9/26/2019] Setup all the environment for the certificates.
                // More info: /iotedge/scripts/linux/runE2ETest.sh : run_test()
                Log.Information("----------------------------------------");
                Log.Information("Install Root Certificate");
                (string rootCertificate, string rootPrivateKey, string rootPassword) = rootCa;
                await OsPlatform.Current.InstallRootCertificateAsync(rootCertificate, rootPrivateKey, rootPassword, scriptPath, token);
            }
            else
            {
                Log.Information("----------------------------------------");
                Log.Information("Skip Root Certificate: Root Certificate is installed");
                Log.Information("----------------------------------------");
            }

            // BEARWASHERE -- CreateDeviceCertificatesAsync(deviceId, scriptPath, token);
            //  Runs > FORCE_NO_PROD_WARNING="true" ${CERT_SCRIPT_DIR}/certGen.sh create_device_certificate "${registration_id}"
            Certificate X509Certificate = await OsPlatform.Current.CreateDeviceCertificatesAsync(deviceId, scriptPath, token);

            // BEARWASHERE -- Read the guild from the DPS team before proceeding
            //await OsPlatform.Current.CreateVerificationCertificatesAsync("BA09350B5E501CAC5431D9D8A79C341792754E1EC92BA62C", scriptPath, token);

            return new CertificateAuthority(X509Certificate, scriptPath);
        }

        public static CertificateAuthority GetQuickstart()
        {
            EdgeCertificates certs = OsPlatform.Current.GetEdgeQuickstartCertificates();
            return new CertificateAuthority(certs);
        }

        CertificateAuthority(EdgeCertificates certs)
        {
            this.Certificates = certs;
            this.scriptPath = Option.None<string>();
        }

        CertificateAuthority(EdgeCertificates certs, string scriptPath)
        {
            this.Certificates = certs;
            this.scriptPath = Option.Maybe(scriptPath);
        }

        CertificateAuthority(Certificate certs, string scriptPath)
        {
            this.genericCertificates = certs;
            this.scriptPath = Option.Maybe(scriptPath);
        }

        public Task<LeafCertificates> GenerateLeafCertificatesAsync(string leafDeviceId, CancellationToken token)
        {
            const string Err = "Cannot generate certificates without script";
            string scriptPath = this.scriptPath.Expect(() => new InvalidOperationException(Err));
            return OsPlatform.Current.GenerateLeafCertificatesAsync(leafDeviceId, scriptPath, token);
        }
    }
}
