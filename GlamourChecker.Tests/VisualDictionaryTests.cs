using System;
using System.Collections.Generic;
using System.IO;
using GlamourChecker.Core;
using Xunit;

namespace GlamourChecker.Tests;

public class VisualDictionaryTests
{
    [Fact]
    public void Constructor_LoadsData_IfValidStreamProvided()
    {
        // SharedModels.json mock content
        string json = @"
        {
            ""12345"": 99
        }";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var dict = new VisualDictionary(stream);

        bool found = dict.TryGetVisualGroup(12345, out var groupId);
        Assert.True(found);
        Assert.Equal(0x1000000000000ul | 99ul, groupId);
    }

    [Fact]
    public void Constructor_HandlesNullStream()
    {
        var dict = new VisualDictionary((Stream)null!);

        bool found = dict.TryGetVisualGroup(12345, out var groupId);
        // Since it loads from resource when null, it might actually find something or nothing
        // but it shouldn't crash!
    }

    [Fact]
    public void Constructor_HandlesNullMap()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("null"));
        var dict = new VisualDictionary(stream);

        bool found = dict.TryGetVisualGroup(12345, out var groupId);
        Assert.False(found);
    }

    [Fact]
    public void Constructor_HandlesInvalidKeysInMap()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"invalid_key\": 123}"));
        var dict = new VisualDictionary(stream);

        bool found = dict.TryGetVisualGroup(12345, out var groupId);
        Assert.False(found);
    }

    private class ThrowingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("Test exception");
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    public VisualDictionaryTests()
    {
        if (Services.PluginLog == null)
        {
            Services.PluginLog = new Moq.Mock<Dalamud.Plugin.Services.IPluginLog>().Object;
        }
    }



    [Fact]
    public void Constructor_HandlesStreamExceptions()
    {
        using var stream = new ThrowingStream();
        var dict = new VisualDictionary(stream);

        bool found = dict.TryGetVisualGroup(12345, out var groupId);
        Assert.False(found);
    }

    [Fact]
    public void TryGetVisualGroup_ReturnsFalse_WhenNotFound()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
        var dict = new VisualDictionary(stream);

        bool found = dict.TryGetVisualGroup(99999, out var groupId);
        Assert.False(found);
        Assert.Equal(0ul, groupId);
    }
}
