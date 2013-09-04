using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace ConsoleApplication1_client_threading
{
    /// <summary>
    /// This class reads length-prefixed messages from a stream until the
    /// stream is closed. This class is thread safe as long as no-one else
    /// is reading from the stream.
    /// </summary>
    class MessageReader
    {
        public delegate void MessageReadHandler(byte[] message);
        public delegate void StreamClosedHandler();

        /// <summary>
        /// This event will be fired whenever a message is complete read from the stream
        /// </summary>
        public event MessageReadHandler MessageRead;

        /// <summary>
        /// This event will be fired when the stream is closed and the reader stops
        /// </summary>
        public event StreamClosedHandler StreamClosed;

        private const int PREFIX_LENGTH = 2;
        private NetworkStream fStream;
        private byte[] fSizeBuffer = new byte[PREFIX_LENGTH];
        private byte[] fBuffer = null;
        private int fBytesRead = 0;

        /// <summary>
        /// Creates a new message reader with the specified stream.
        /// </summary>
        /// <param name="stream">The stream from which to read messages</param>
        public MessageReader(NetworkStream stream)
        {
            fStream = stream;
        }

        /// <summary>
        /// Call this message to begin receiving messages. Simply close the
        /// stream to stop receiving. Calling this more than once is an error.
        /// </summary>
        public void Start()
        {
            fStream.BeginRead(fSizeBuffer, 0, fSizeBuffer.Length, FinishReadSize, null);
        }

        private void OnStreamClosed()
        {
            if (null != StreamClosed)
                StreamClosed();
        }

        private void OnMessageRead(byte[] message)
        {
            // Ignore errors by the handlers
            try
            {
                if (null != MessageRead)
                    MessageRead(message);
            }
            catch
            {
            }
        }

        private void FinishReadSize(IAsyncResult result)
        {
            try
            {
                // Read precisely four bytes for the length of the following message
                int read = fStream.EndRead(result);
                if (fSizeBuffer.Length != read)
                    throw new Exception();
                Array.Reverse(fSizeBuffer);
                int size = BitConverter.ToInt16(fSizeBuffer, 0);

                // Create a buffer to hold the message and start reading it.
                fBytesRead = 0;
                fBuffer = new byte[size];
                fStream.BeginRead(fBuffer, 0, fBuffer.Length, FinishRead, null);
            }
            catch
            {
                OnStreamClosed();
            }
        }

        private void FinishRead(IAsyncResult result)
        {
            try
            {
                // Finish reading from our stream. 0 bytes read means stream was closed
                int read = fStream.EndRead(result);
                if (0 == read)
                    throw new Exception();

                // Increment the number of bytes we've read. If there's still more to get, get them
                fBytesRead += read;
                if (fBytesRead < fBuffer.Length)
                {
                    fStream.BeginRead(fBuffer, fBytesRead, fBuffer.Length - fBytesRead, FinishRead, null);
                    return;
                }

                // Should be exactly the right number read now.
                if (fBytesRead != fBuffer.Length)
                    throw new Exception();

                // Handle the message and go get the next one.
                OnMessageRead(fBuffer);
                fStream.BeginRead(fSizeBuffer, 0, fSizeBuffer.Length, FinishReadSize, null);
            }
            catch
            {
                OnStreamClosed();
            }
        }
    }

}
