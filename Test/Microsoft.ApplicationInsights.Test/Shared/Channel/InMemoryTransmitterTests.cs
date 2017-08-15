﻿namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
#if NET40 || NET45 || NET46 || NETCOREAPP1_1
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    
    using Extensibility;
#if !NET40
    using System.Net.Http;
#endif
    using System.Threading;

    public class InMemoryTransmitterTests
    {
        [TestClass]
        public class SendingInterval
        {
            [TestMethod]
            public void DefaultValueIsAppropriateForProductionEnvironmentAndUnitTests()
            {
                var transmitter = new InMemoryTransmitter(new TelemetryBuffer());
                Assert.AreEqual(TimeSpan.FromSeconds(30), transmitter.SendingInterval);
            }

            [TestMethod]
            public void CanBeChangedByChannelToTunePerformance()
            {
                var transmitter = new InMemoryTransmitter(new TelemetryBuffer());

                var expectedValue = TimeSpan.FromSeconds(42);
                transmitter.SendingInterval = expectedValue;

                Assert.AreEqual(expectedValue, transmitter.SendingInterval);
            }

#if !NET40
            private class TelemetryBufferWithInternalOperationValidation : TelemetryBuffer
            {
                public bool WasCalled = false;

                public override IEnumerable<ITelemetry> Dequeue()
                {
                    Assert.IsTrue(SdkInternalOperationsMonitor.IsEntered());
                    HttpClient client = new HttpClient();
                    var task = client.GetStringAsync("http://bing.com").ContinueWith((result) => { Assert.IsTrue(SdkInternalOperationsMonitor.IsEntered()); });

                    task.Wait();

                    WasCalled = true;
                    return base.Dequeue();
                }
            }

            [TestMethod]
            public void SendingLogicMarkedAsInternalSdkOperation()
            {
                var buffer = new TelemetryBufferWithInternalOperationValidation();
                var transmitter = new InMemoryTransmitter(buffer);
                buffer.OnFull();

                for (int i = 0; i < 10; i++)
                {
                    if (buffer.WasCalled)
                    {
                        break;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                Assert.IsTrue(buffer.WasCalled);
            }

            [TestMethod]
            public void FlushMarkedAsInternalSdkOperation()
            {
                var buffer = new TelemetryBufferWithInternalOperationValidation();
                var transmitter = new InMemoryTransmitter(buffer);
                transmitter.Flush(TimeSpan.FromSeconds(1));

                for (int i = 0; i < 10; i++)
                {
                    if (buffer.WasCalled)
                    {
                        break;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                Assert.IsTrue(buffer.WasCalled);
            }
#endif
        }
    }
}
