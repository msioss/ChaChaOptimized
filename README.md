# ChaChaOptimization

**Швидка, безпечна та оптимізована .NET-реалізація алгоритму шифрування ChaCha8/12/20 для малопотужних або ресурсно-обмежених пристроїв.**

## Основні можливості

- ChaCha8, ChaCha12, ChaCha20 (кількість раундів обирається)
- Нативна підтримка .NET 6+, не використовує unsafe/неуправляєму пам'ять
- Ефективне шифрування потоку даних: IoT, embedded, вебкамера, мобільні клієнти
- Оптимізовано для роботи в Docker, Linux та Windows
- Простий API — чистий C#, без сторонніх залежностей
- Висока швидкість та низьке енергоспоживання (на практиці ~2–2.5x швидше за ChaCha20 при ChaCha8/12)

## Використання

### Базове шифрування та дешифрування
using ChaChaOptimization;

// Генерація ключа та nonce
byte[] key = new byte; // 256-бітний ключ
byte[] nonce = new byte; // 96-бітний nonce​
uint counter = 1; // лічильник (початковий блок)
int rounds = 12; // можна виставити 8/12/20 для оптимізації

var random = new Random();
random.NextBytes(key);
random.NextBytes(nonce);

// Дані для шифрування
byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello, world!");

// Створення шифру
var cipher = new ChaCha(key, nonce, counter, rounds);
// Шифрування (XOR-stream)
cipher.Process(data);

// Дешифрування
// (повторно з тим самим ключем/nonce/counter)
var decryptCipher = new ChaCha(key, nonce, counter, rounds);
decryptCipher.Process(data);

string message = System.Text.Encoding.UTF8.GetString(data);
Console.WriteLine(message); // "Hello, world!"

### Використання в малопотужних системах (Docker, ARM, Linux)
Dockerfile (фрагмент)
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine
WORKDIR /app
COPY . .
RUN dotnet build -c Release
CMD ["dotnet", "run", "-c", "Release"]

Результати бенчмарків дивись у прикладі [benchmark-results.csv] — автоматично підраховується speedup стосовно ChaCha20.

## API

- **ChaCha(byte[] key, byte[] nonce, uint counter, int rounds)**
    - Створює потоковий шифр з вказаними параметрами
    - Ключ — 32 байти, nonce — 12 байт
    - rounds: 8/12/20

- **void Process(Span<byte> data)**
    - Шифрує або дешифрує дані in-place, XOR'ячи з потоковим шифром

## Ліцензія

MIT — можеш використовувати для будь-яких цілей, у тому числі комерційних та освітніх.

## Додатково

- Оціній прискорення на своїй платформі і обирай потрібну кількість раундів за балансом безпеки/швидкості.
- Для інтеграції з ASP.NET Core просто підключи клас як DI-сервіс для потокового шифрування.
