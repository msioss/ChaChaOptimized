using System;
using System.Runtime.CompilerServices;

namespace ChaChaOptimization;

public sealed class ChaCha
{
    private readonly byte[] key;
    private readonly byte[] nonce;
    private readonly uint counter;
    private readonly int rounds;

    public ChaCha(byte[] key, byte[] nonce, uint counter, int rounds)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes");
        if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes");
        if (rounds is not (8 or 12 or 20)) throw new ArgumentException("Rounds must be 8, 12, or 20");

        this.key = key;
        this.nonce = nonce;
        this.counter = counter;
        this.rounds = rounds;
    }

    /// <summary>
    /// Шифрує дані на місці за допомогою XOR з keystream.
    /// </summary>
    public void Process(Span<byte> data)
    {
        int length = data.Length;
        int offset = 0;
        
        Span<uint> x = stackalloc uint[16];        // Робочий state
        Span<uint> state = stackalloc uint[16];    // Постійний state
        Span<byte> keystream = stackalloc byte[64]; // Буфер для keystream
        
        InitializeState(state);

        while (offset < length)
        {
            // Копіюємо state в x для обробки
            state.CopyTo(x);

            // Виконуємо раунди ChaCha (розгортка по 2)
            for (int i = rounds; i > 0; i -= 2)
            {
                // Column rounds
                QuarterRound(ref x[0], ref x[4], ref x[8], ref x[12]);
                QuarterRound(ref x[1], ref x[5], ref x[9], ref x[13]);
                QuarterRound(ref x[2], ref x[6], ref x[10], ref x[14]);
                QuarterRound(ref x[3], ref x[7], ref x[11], ref x[15]);
                
                // Diagonal rounds
                QuarterRound(ref x[0], ref x[5], ref x[10], ref x[15]);
                QuarterRound(ref x[1], ref x[6], ref x[11], ref x[12]);
                QuarterRound(ref x[2], ref x[7], ref x[8], ref x[13]);
                QuarterRound(ref x[3], ref x[4], ref x[9], ref x[14]);
            }

            // Додаємо оригінальний state
            for (int i = 0; i < 16; i++)
                x[i] += state[i];

            // Записуємо keystream
            WriteKeystream(keystream, x);

            // XOR з даними
            int blockSize = Math.Min(64, length - offset);
            for (int i = 0; i < blockSize; i++)
                data[offset + i] ^= keystream[i];

            offset += blockSize;
            state[12]++; // Інкремент лічильника блоків
        }
    }

    private void InitializeState(Span<uint> state)
    {
        // ChaCha constants: "expand 32-byte k"
        state[0] = 0x61707865;
        state[1] = 0x3320646e;
        state[2] = 0x79622d32;
        state[3] = 0x6b206574;

        // Ключ (32 байти = 8 слів)
        for (int i = 0; i < 8; i++)
        {
            state[4 + i] = ReadUInt32LE(key, i * 4);
        }

        // Лічильник блоків
        state[12] = counter;
        
        // Nonce (12 байтів = 3 слова)
        state[13] = ReadUInt32LE(nonce, 0);
        state[14] = ReadUInt32LE(nonce, 4);
        state[15] = ReadUInt32LE(nonce, 8);
    }

    /// <summary>
    /// Безпечно читає 32-бітне число у форматі little-endian.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32LE(byte[] data, int offset)
    {
        return (uint)(data[offset] | 
                     (data[offset + 1] << 8) | 
                     (data[offset + 2] << 16) | 
                     (data[offset + 3] << 24));
    }

    /// <summary>
    /// Записує 64-байтний keystream з state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteKeystream(Span<byte> ks, Span<uint> x)
    {
        int k = 0;
        for (int i = 0; i < 16; i++)
        {
            uint v = x[i];
            ks[k++] = (byte)v;
            ks[k++] = (byte)(v >> 8);
            ks[k++] = (byte)(v >> 16);
            ks[k++] = (byte)(v >> 24);
        }
    }

    /// <summary>
    /// Quarter-round операція ChaCha з ротаціями: 16, 12, 8, 7.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        a += b; d ^= a; d = (d << 16) | (d >> 16);
        c += d; b ^= c; b = (b << 12) | (b >> 20);
        a += b; d ^= a; d = (d << 8) | (d >> 24);
        c += d; b ^= c; b = (b << 7) | (b >> 25);
    }
}