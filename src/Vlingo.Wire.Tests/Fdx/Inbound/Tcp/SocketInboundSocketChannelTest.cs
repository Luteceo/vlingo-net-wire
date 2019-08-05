// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Linq;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Actors.TestKit;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Fdx.Inbound.Tcp;
using Vlingo.Wire.Fdx.Outbound.Tcp;
using Vlingo.Wire.Message;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Inbound.Tcp
{
    using Vlingo.Wire.Node;
    
    public class SocketInboundSocketChannelTest: IDisposable
    {
        private const string AppMessage = "APP TEST ";
        private const string OpMessage = "OP TEST ";

        private static int _testPort = 37673;

        private readonly ManagedOutboundSocketChannel _appChannel;
        private readonly IChannelReader _appReader;
        private readonly ManagedOutboundSocketChannel _opChannel;
        private readonly IChannelReader _opReader;

        [Fact]
        public void TestOpInboundChannel()
        {
            var consumer = new MockChannelReaderConsumer("consume");
            var consumeCount = 0;
            var accessSafely = AccessSafely.Immediately()
                .WritingWith<int>("consume", (value) => consumeCount += value)
                .ReadingWith("consume", () => consumeCount);
            consumer.UntilConsume = accessSafely;
            
            _opReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = OpMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _opChannel.Write(rawMessage1.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 1, _opReader);
            
            Assert.Equal(1, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = OpMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _opChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 2, _opReader);
            
            Assert.Equal(2, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message2, consumer.Messages.Last());
        }

        [Fact]
        public void TestAppInboundChannel()
        {
            var consumer = new MockChannelReaderConsumer("consume");
            var consumeCount = 0;
            var accessSafely = AccessSafely.Immediately()
                .WritingWith<int>("consume", (value) => consumeCount += value)
                .ReadingWith("consume", () => consumeCount);
            consumer.UntilConsume = accessSafely;
            
            _appReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = AppMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _appChannel.Write(rawMessage1.AsStream(buffer));

            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 1, _appReader);
            
            Assert.Equal(1, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = AppMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _appChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 2, _appReader);
            
            Assert.Equal(2, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message2, consumer.Messages.Last());
        }

        public SocketInboundSocketChannelTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            var node = Node.With(Id.Of(2), Name.Of("node2"), Host.Of("localhost"), _testPort, _testPort + 1);
            var logger = ConsoleLogger.TestInstance();
            _opChannel = new ManagedOutboundSocketChannel(node, node.OperationalAddress, logger);
            _appChannel = new ManagedOutboundSocketChannel(node, node.ApplicationAddress, logger);
            _opReader = new SocketChannelInboundReader(node.OperationalAddress.Port, "test-op", 1024, logger);
            _appReader = new SocketChannelInboundReader(node.ApplicationAddress.Port, "test-app", 1024, logger);
            ++_testPort;
        }

        public void Dispose()
        {
            _appChannel.Dispose();
            _opChannel.Dispose();
            _appReader.Close();
            _opReader.Close();
        }

        private void ProbeUntilConsumed(Func<bool> reading, IChannelReader reader)
        {
            do
            {
                reader.ProbeChannel();
            } while (reading());
        }
    }
}