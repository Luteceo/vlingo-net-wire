// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Fdx.Bidirectional;
using Vlingo.Wire.Fdx.Bidirectional.Netty.Client;
using Vlingo.Wire.Node;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Bidirectional.Netty.Server
{
    public sealed class NettyServerChannelActorTest : BaseServerChannelTest
    {
        private static readonly AtomicInteger TestPort = new AtomicInteger(37480);
        
        public NettyServerChannelActorTest(ITestOutputHelper output) : base(output, 100)
        {
        }

        protected override IClientRequestResponseChannel GetClient(IResponseChannelConsumer consumer, int maxBufferPoolSize,
            int maxMessageSize, ILogger logger) =>
            new NettyClientRequestResponseChannel(Address.From(Host.Of("localhost"), TestPort.Get(), AddressType.None), consumer, maxBufferPoolSize, maxMessageSize, logger);

        protected override IServerRequestResponseChannel GetServer(Stage stage, IRequestChannelConsumerProvider provider, string name,
            int processorPoolSize, int maxBufferPoolSize, int maxMessageSize, long probeInterval, long probeTimeout) =>
            ServerRequestResponseChannelFactory.StartNetty(
                stage,
                provider,
                TestPort.IncrementAndGet(),
                "test-server",
                processorPoolSize,
                maxBufferPoolSize,
                maxMessageSize,
                probeInterval,
                probeTimeout);
    }
}