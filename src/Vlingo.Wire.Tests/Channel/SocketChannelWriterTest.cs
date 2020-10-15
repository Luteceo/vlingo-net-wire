// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Fdx.Inbound;
using Vlingo.Wire.Message;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Channel
{
    using Vlingo.Wire.Node;
    
    public class SocketChannelWriterTest : IDisposable
    {
        private static readonly string TestMessage = "TEST ";

        private readonly SocketChannelWriter _channelWriter;
        private readonly IChannelReader _channelReader;
        
        [Fact]
        public void TestChannelWriter()
        {
            var consumer = new MockChannelReaderConsumer();
            var accessSafely = consumer.AfterCompleting(2);

            _channelReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = TestMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _channelWriter.Write(rawMessage1, buffer);
            
            ProbeUntilConsumed(() => accessSafely.ReadFromNow<int>("count") < 1, _channelReader, 10);
            
            Assert.Equal(1, accessSafely.ReadFromNow<int>("count"));
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = TestMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _channelWriter.Write(rawMessage2, buffer);

            ProbeUntilConsumed(() => accessSafely.ReadFromNow<int>("count") < 2, _channelReader, 10);
            
            Assert.Equal(2, accessSafely.ReadFromNow<int>("count"));
            Assert.Equal(message2, consumer.Messages.Last());
            
        }

        public SocketChannelWriterTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            
            var node = Node.With(Id.Of(2), Name.Of("node2"), Host.Of("localhost"), 37377, 37378);
            var logger = ConsoleLogger.TestInstance();
            _channelWriter = new SocketChannelWriter(node.OperationalAddress, logger);
            _channelReader = new SocketChannelInboundReader(node.OperationalAddress.Port, "test-reader", 1024, logger);
        }

        public void Dispose()
        {
            _channelWriter.Close();
            _channelReader.Close();
        }
        
        private void ProbeUntilConsumed(Func<bool> reading, IChannelReader reader, int maxSeconds)
        {
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                reader.ProbeChannel();
            } while (reading() || sw.Elapsed.TotalSeconds >= maxSeconds);
            sw.Stop();
        }
    }
}