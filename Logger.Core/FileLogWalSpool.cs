using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Logger.Core.Models;

namespace Logger.Core
{
    internal sealed class FileLogWalSpool : IDisposable
    {
        private const int RecordHeaderSize = 4;
        private const int EntryHeaderSize = 16;
        private const int ReadBufferSize = 4096;
        private const long CompactThresholdBytes = 256 * 1024;
        private static readonly UTF8Encoding FileEncoding = new UTF8Encoding(false);

        private readonly object _syncRoot = new object();
        private readonly string _walFilePath;
        private readonly string _checkpointFilePath;
        private readonly string _spoolDirectoryPath;
        private readonly LogSpoolFlushMode _flushMode;
        private long _committedOffset;
        private int _disposed;
        private FileStream _writeStream;

        public FileLogWalSpool(LogStorageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _spoolDirectoryPath = LoggerPathUtility.BuildSpoolDirectoryPath(
                context.LoggerName,
                context.SpoolRootDirectoryPath);
            _walFilePath = LoggerPathUtility.BuildSpoolFilePath(
                context.LoggerName,
                context.SpoolRootDirectoryPath);
            _checkpointFilePath = LoggerPathUtility.BuildSpoolCheckpointPath(
                context.LoggerName,
                context.SpoolRootDirectoryPath);
            _flushMode = context.SpoolFlushMode;

            Initialize();
        }

        public bool HasPendingEntries
        {
            get
            {
                lock (_syncRoot)
                {
                    ThrowIfDisposed();
                    return GetWalLengthUnsafe() > _committedOffset;
                }
            }
        }

        public void AppendEntries(IReadOnlyList<LogEntry> entries, bool requireDurableFlush)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            lock (_syncRoot)
            {
                ThrowIfDisposed();
                Directory.CreateDirectory(_spoolDirectoryPath);
                EnsureWriteStreamUnsafe();

                using (MemoryStream buffer = new MemoryStream(entries.Count * 64))
                {
                    for (int index = 0; index < entries.Count; index++)
                    {
                        LogEntry entry = entries[index];
                        if (entry == null)
                        {
                            continue;
                        }

                        WriteEntryUnsafe(buffer, entry);
                    }

                    if (buffer.Length <= 0)
                    {
                        return;
                    }

                    _writeStream.Write(buffer.GetBuffer(), 0, (int)buffer.Length);
                    FlushWriteStreamUnsafe(requireDurableFlush);
                }
            }
        }

        public bool TryReadNextBatch(int maxCount, out List<LogEntry> entries, out long commitOffset)
        {
            entries = new List<LogEntry>();
            commitOffset = 0L;

            if (maxCount <= 0)
            {
                return false;
            }

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                long fileLength = GetWalLengthUnsafe();
                if (fileLength <= _committedOffset)
                {
                    return false;
                }

                List<byte> lineBuffer = new List<byte>(256);

                using (FileStream stream = new FileStream(
                    _walFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite))
                {
                    stream.Seek(_committedOffset, SeekOrigin.Begin);
                    while (entries.Count < maxCount)
                    {
                        LogEntry entry;
                        if (!TryReadEntryUnsafe(stream, out entry))
                        {
                            break;
                        }

                        if (entry != null)
                        {
                            entries.Add(entry);
                        }

                        commitOffset = stream.Position;
                    }
                }

                return commitOffset > _committedOffset;
            }
        }

        public void MarkCommitted(long commitOffset)
        {
            if (commitOffset <= 0L)
            {
                return;
            }

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                long fileLength = GetWalLengthUnsafe();
                if (fileLength <= 0L)
                {
                    ResetSpoolUnsafe();
                    return;
                }

                long normalizedCommitOffset = commitOffset > fileLength
                    ? fileLength
                    : commitOffset;

                if (normalizedCommitOffset <= _committedOffset)
                {
                    return;
                }

                _committedOffset = normalizedCommitOffset;
                WriteCheckpointUnsafe(_committedOffset);
                CompactIfNeededUnsafe(fileLength);
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                CloseWriteStreamUnsafe();
            }

            _disposed = 1;
        }

        private void Initialize()
        {
            lock (_syncRoot)
            {
                Directory.CreateDirectory(_spoolDirectoryPath);
                TrimPartialTrailingRecordUnsafe();

                long fileLength = GetWalLengthUnsafe();
                _committedOffset = ReadCheckpointUnsafe();

                if (_committedOffset < 0L || _committedOffset > fileLength)
                {
                    _committedOffset = 0L;
                    WriteCheckpointUnsafe(_committedOffset);
                }

                if (fileLength <= 0L)
                {
                    ResetSpoolUnsafe();
                }
                else
                {
                    EnsureWriteStreamUnsafe();
                }
            }
        }

        private void TrimPartialTrailingRecordUnsafe()
        {
            using (FileStream stream = new FileStream(
                _walFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite))
            {
                if (stream.Length <= 0L)
                {
                    return;
                }

                long validLength = GetValidWalLengthUnsafe(stream);
                if (validLength == stream.Length)
                {
                    return;
                }

                stream.SetLength(validLength);
                FlushStream(stream, false);
            }
        }

        private long GetWalLengthUnsafe()
        {
            if (_writeStream != null)
            {
                return _writeStream.Length;
            }

            return File.Exists(_walFilePath)
                ? new FileInfo(_walFilePath).Length
                : 0L;
        }

        private long ReadCheckpointUnsafe()
        {
            if (!File.Exists(_checkpointFilePath))
            {
                return 0L;
            }

            string text = File.ReadAllText(_checkpointFilePath).Trim();
            long offset;
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out offset)
                ? offset
                : 0L;
        }

        private void WriteCheckpointUnsafe(long offset)
        {
            Directory.CreateDirectory(_spoolDirectoryPath);

            using (FileStream stream = new FileStream(
                _checkpointFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite))
            {
                byte[] buffer = FileEncoding.GetBytes(offset.ToString(CultureInfo.InvariantCulture));
                stream.Write(buffer, 0, buffer.Length);
                FlushStream(stream, false);
            }
        }

        private void ResetSpoolUnsafe()
        {
            _committedOffset = 0L;
            CloseWriteStreamUnsafe();

            if (File.Exists(_checkpointFilePath))
            {
                File.Delete(_checkpointFilePath);
            }

            if (File.Exists(_walFilePath))
            {
                File.Delete(_walFilePath);
            }
        }

        private void CompactIfNeededUnsafe(long fileLength)
        {
            if (_committedOffset <= 0L)
            {
                return;
            }

            if (_committedOffset >= fileLength)
            {
                ResetSpoolUnsafe();
                return;
            }

            if (_committedOffset < CompactThresholdBytes && (_committedOffset * 2L) < fileLength)
            {
                return;
            }

            string tempFilePath = _walFilePath + ".tmp";
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            CloseWriteStreamUnsafe();
            using (FileStream source = new FileStream(
                _walFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            using (FileStream target = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                source.Seek(_committedOffset, SeekOrigin.Begin);
                source.CopyTo(target);
                FlushStream(target, false);
            }

            File.Delete(_walFilePath);
            File.Move(tempFilePath, _walFilePath);

            _committedOffset = 0L;
            WriteCheckpointUnsafe(_committedOffset);
            EnsureWriteStreamUnsafe();
        }

        private void EnsureWriteStreamUnsafe()
        {
            if (_writeStream != null)
            {
                return;
            }

            _writeStream = new FileStream(
                _walFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                ReadBufferSize,
                FileOptions.SequentialScan);
            _writeStream.Seek(0L, SeekOrigin.End);
        }

        private void CloseWriteStreamUnsafe()
        {
            if (_writeStream == null)
            {
                return;
            }

            _writeStream.Dispose();
            _writeStream = null;
        }

        private long GetValidWalLengthUnsafe(FileStream stream)
        {
            stream.Seek(0L, SeekOrigin.Begin);
            long validLength = 0L;

            while (stream.Position < stream.Length)
            {
                long recordStart = stream.Position;
                int recordLength;
                if (!TryReadRecordLengthUnsafe(stream, out recordLength))
                {
                    break;
                }

                if (recordLength < EntryHeaderSize || stream.Length - stream.Position < recordLength)
                {
                    break;
                }

                stream.Seek(recordLength, SeekOrigin.Current);
                validLength = stream.Position;

                if (validLength <= recordStart)
                {
                    break;
                }
            }

            return validLength;
        }

        private static void WriteEntryUnsafe(Stream stream, LogEntry entry)
        {
            byte[] messageBytes = FileEncoding.GetBytes(entry != null ? entry.Message ?? string.Empty : string.Empty);
            int recordLength = EntryHeaderSize + messageBytes.Length;

            WriteInt32Unsafe(stream, recordLength);
            WriteInt64Unsafe(stream, entry != null ? entry.Timestamp.ToBinary() : DateTime.Now.ToBinary());
            WriteInt32Unsafe(stream, (int)(entry != null ? entry.Level : LogLevel.Info));
            WriteInt32Unsafe(stream, messageBytes.Length);
            if (messageBytes.Length > 0)
            {
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
        }

        private static bool TryReadEntryUnsafe(FileStream stream, out LogEntry entry)
        {
            entry = null;
            long recordStart = stream.Position;
            int recordLength;
            if (!TryReadRecordLengthUnsafe(stream, out recordLength))
            {
                stream.Seek(recordStart, SeekOrigin.Begin);
                return false;
            }

            if (recordLength < EntryHeaderSize || stream.Length - stream.Position < recordLength)
            {
                stream.Seek(recordStart, SeekOrigin.Begin);
                return false;
            }

            long timestampBinary = ReadInt64Unsafe(stream);
            int levelValue = ReadInt32Unsafe(stream);
            int messageLength = ReadInt32Unsafe(stream);
            int remainingLength = recordLength - EntryHeaderSize;
            if (messageLength < 0 || messageLength > remainingLength)
            {
                stream.Seek(recordStart + RecordHeaderSize + recordLength, SeekOrigin.Begin);
                return false;
            }

            byte[] messageBytes = messageLength > 0 ? ReadBytesUnsafe(stream, messageLength) : Array.Empty<byte>();
            int extraLength = remainingLength - messageLength;
            if (extraLength > 0)
            {
                stream.Seek(extraLength, SeekOrigin.Current);
            }

            entry = new LogEntry(
                DateTime.FromBinary(timestampBinary),
                (LogLevel)levelValue,
                FileEncoding.GetString(messageBytes, 0, messageBytes.Length));
            return true;
        }

        private static bool TryReadRecordLengthUnsafe(FileStream stream, out int recordLength)
        {
            recordLength = 0;
            if (stream.Length - stream.Position < RecordHeaderSize)
            {
                return false;
            }

            recordLength = ReadInt32Unsafe(stream);
            return true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed != 0)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void FlushWriteStreamUnsafe(bool forceDurableFlush)
        {
            FlushStream(_writeStream, forceDurableFlush);
        }

        private void FlushStream(FileStream stream, bool forceDurableFlush)
        {
            if (stream == null)
            {
                return;
            }

            if (!forceDurableFlush && _flushMode != LogSpoolFlushMode.Durable)
            {
                stream.Flush();
                return;
            }

            try
            {
                stream.Flush(true);
            }
            catch (PlatformNotSupportedException)
            {
                stream.Flush();
            }
            catch (NotSupportedException)
            {
                stream.Flush();
            }
        }

        private static void WriteInt32Unsafe(Stream stream, int value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
        }

        private static void WriteInt64Unsafe(Stream stream, long value)
        {
            WriteInt32Unsafe(stream, (int)(value & 0xFFFFFFFF));
            WriteInt32Unsafe(stream, (int)((value >> 32) & 0xFFFFFFFF));
        }

        private static int ReadInt32Unsafe(Stream stream)
        {
            int b0 = stream.ReadByte();
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            int b3 = stream.ReadByte();

            if (b0 < 0 || b1 < 0 || b2 < 0 || b3 < 0)
            {
                throw new EndOfStreamException();
            }

            return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
        }

        private static long ReadInt64Unsafe(Stream stream)
        {
            uint low = unchecked((uint)ReadInt32Unsafe(stream));
            uint high = unchecked((uint)ReadInt32Unsafe(stream));
            return ((long)high << 32) | low;
        }

        private static byte[] ReadBytesUnsafe(Stream stream, int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int bytesRead = stream.Read(buffer, offset, length - offset);
                if (bytesRead <= 0)
                {
                    throw new EndOfStreamException();
                }

                offset += bytesRead;
            }

            return buffer;
        }
    }
}
