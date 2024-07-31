// Copyright (c) Microsoft Corporation. All rights reserved.
using System.Text;

namespace Microsoft.DotNet.Scaffolding.Internal.CliHelpers;

internal sealed class ProcessOutputStreamReader : IDisposable
{
    private const char FlushBuilderCharacter = '\n';

    private static readonly char[] IgnoreCharacters = [ '\r' ];

    private StringBuilder? _builder;
    private StringWriter? _capture;

    public string? CapturedOutput => _capture?.GetStringBuilder()?.ToString();

    public ProcessOutputStreamReader Capture()
    {
        ThrowIfCaptureSet();

        _capture = new StringWriter();

        return this;
    }

    public Task BeginRead(TextReader reader)
    {
        return Task.Run(() => Read(reader));
    }

    public void Read(TextReader reader)
    {
        var bufferSize = 1;

        char currentCharacter;

        var buffer = new char[bufferSize];
        _builder = new StringBuilder();

        if (reader is not null)
        {
            // Using Read with buffer size 1 to prevent looping endlessly
            // like we would when using Read() with no buffer
            while (reader.Read(buffer, 0, bufferSize) > 0)
            {
                currentCharacter = buffer[0];

                if (currentCharacter == FlushBuilderCharacter)
                {
                    WriteBuilder();
                }
                else if (!IgnoreCharacters.Contains(currentCharacter))
                {
                    _builder.Append(currentCharacter);
                }
            }
        }

        // Flush anything else when the stream is closed
        // Which should only happen if someone used console.Write
        WriteBuilder();

        void WriteBuilder()
        {
            if (_builder.Length == 0)
            {
                return;
            }

            WriteLine(_builder.ToString());
            _builder.Clear();
        }
    }

    private void WriteLine(string str)
    {
        _capture?.WriteLine(str);
    }

    private void ThrowIfCaptureSet()
    {
        if (_capture != null)
        {
            throw new InvalidOperationException("Already capturing stream!"); // TODO: Localize this?
        }
    }

    public void Dispose()
    {
        _capture?.Dispose();
        _capture = null;
    }
}
