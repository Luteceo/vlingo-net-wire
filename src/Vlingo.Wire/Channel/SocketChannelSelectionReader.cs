// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Net.Sockets;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public class SocketChannelSelectionReader: SelectionReader
    {
        public SocketChannelSelectionReader(ChannelMessageDispatcher dispatcher) : base(dispatcher)
        {
        }

        public override void Read(Socket channel, RawMessageBuilder builder)
        {
            var buffer = builder.WorkBuffer();
            var bytes = new byte[buffer.Length];
            var state = new StateObject(channel, buffer, bytes, builder);
            channel.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, ReceiveCallback, state);

            Dispatcher.DispatchMessageFor(builder);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            var client = state.WorkSocket;
            var buffer = state.Buffer;
            var bytes = state.Bytes;
            var builder = state.Builder;

            var bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                buffer.Write(bytes, state.TotalRead, bytesRead);
                state.TotalRead += bytesRead;
            }

            int bytesRemain = client.Available;
            if (bytesRemain > 0)
            {
                client.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None , ReceiveCallback, state);
            }
            else
            {
                if (bytesRead > 0)
                {
                    Dispatcher.DispatchMessageFor(builder);
                }
            }
        }
        
        private class StateObject
        {
            public StateObject(Socket workSocket, Stream buffer, byte[] bytes, RawMessageBuilder builder)
            {
                WorkSocket = workSocket;
                Buffer = buffer;
                Bytes = bytes;
                Builder = builder;
            }
            
            public Socket WorkSocket { get; }
            
            public Stream Buffer { get; }
            
            public byte[] Bytes { get; }
            
            public RawMessageBuilder Builder { get; }
            
            public int TotalRead { get; set; }
        }
    }
}