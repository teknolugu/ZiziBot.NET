using System;
using System.IO;
using System.Threading.Tasks;
using MongoDB.Entities;
using Serilog;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

namespace WinTenDev.Zizi.Models.Misc;

// Reference: https://github.com/wiz0u/WTelegramClient/blob/master/Examples/Program_Heroku.cs#L61
public class WTelegramSessionMongoDbStorageStream : Stream
{
    private readonly string _sessionName;
    private byte[] _data;
    private int _dataLen;
    private DateTime _lastWrite;
    private Task _delayedWrite;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _dataLen;
    public override long Position { get => 0; set {} }

    public WTelegramSessionMongoDbStorageStream(string sessionName)
    {
        _sessionName = sessionName;

        GetSessionAsync().Wait();
    }

    private async Task GetSessionAsync()
    {
        Log.Information("Getting session: {SessionName}", _sessionName);

        var session = await DB.Find<WTelegramSession>()
            .Match(session => session.SessionName == _sessionName)
            .ExecuteFirstAsync();

        if (session != null)
        {
            _data = session.SessionData;
            _dataLen = _data.Length;
        }
    }

    private async Task SaveSessionAsync(WTelegramSession wTelegramSession)
    {
        Log.Information("Saving session: {SessionName}", _sessionName);

        var wTelegramSessions = await DB.Find<WTelegramSession>()
            .Match(telegramSession => telegramSession.SessionName == _sessionName)
            .ExecuteAsync();

        if (wTelegramSessions.Count > 0)
        {
            Log.Debug("Updating WTelegram session for {SessionName}", _sessionName);
            await DB.Update<WTelegramSession>()
                .Match(telegramSession => telegramSession.SessionName == _sessionName)
                .Modify(session => session.SessionData, wTelegramSession.SessionData)
                .ExecuteAsync();
        }
        else
        {
            Log.Debug("Adding WTelegram session for {SessionName}", _sessionName);
            await wTelegramSession.InsertAsync();
        }

        Log.Information("Saved session successfully. Session name: {SessionName}", _sessionName);
    }

    public override int Read(
        byte[] buffer,
        int offset,
        int count
    )
    {
        Array.Copy(_data, 0, buffer, offset, count);

        return count;
    }

    public override void Write(
        byte[] buffer,
        int offset,
        int count
    )
    {
        _data = buffer;
        _dataLen = count;
        if (_delayedWrite != null) return;
        var left = 1000 - (int)(DateTime.UtcNow - _lastWrite).TotalMilliseconds;
        if (left < 0)
        {
            var wTelegramSession = new WTelegramSession()
            {
                SessionName = _sessionName,
                SessionData = count == buffer.Length ? buffer : buffer[offset..(offset + count)]
            };

            SaveSessionAsync(wTelegramSession).Wait();

            _lastWrite = DateTime.UtcNow;
        }
        else// delay writings for a full second
        {
            _delayedWrite = Task.Delay(left).ContinueWith(t => {
                lock (this)
                {
                    _delayedWrite = null;
                    Write(_data, 0, _dataLen);
                }
            });
        }
    }

    public override void Flush() {}
    public override void SetLength(long value) {}

    public override long Seek(
        long offset,
        SeekOrigin origin) => 0;
}