🌐 [English](README.md) | [Українська](README_UA.md)
# Localization System Mini для Unity
[МОЖЛИВОСТІ](#можливості) | [ШВИДКИЙ СТАРТ](#швидкий-старт) | [ФОРМАТ CSV](#формат-csv) | [ШРИФТИ](#керування-шрифтами) | [API](#довідник-api) | [ДЕМО СЦЕНА](#приклад-сцени) | [ПРИМІТКИ](#примітки) | [ЛІЦЕНЗІЯ](#ліцензія) | [ПІДТРИМКА](#підтримка)

<p align="center">
  <img width="30%" alt="Імпорт CSV таблиці" src="https://github.com/user-attachments/assets/0001a6ec-6736-460a-b35c-de63d1625ee5" />
  <img width="30%" alt="Налаштування ScriptableObject" src="https://github.com/user-attachments/assets/e67125dc-90b6-472c-be7d-437ecc495f95" />  
  <img width="30%" alt="Демонстрація викликів методів" src="https://github.com/user-attachments/assets/669d0aff-1462-4d37-9a2e-8d83727a6399" />
</p>

Проста система локалізації тексту на основі CSV для проєктів Unity з використанням TextMeshPro. Читає CSV-файл, повертає рядки, змінює шрифти та кешує все для оптимізації. Створено для соло-розробників та інді-команд, які хочуть тримати текстові значення поза скриптами й керувати перекладами через зовнішні редактори.

> **Джерело:** Це спрощена версія системи локалізації з асету [Automatic Tutorial Maker](https://u3d.as/3tsL).

## Можливості

- **Переклади на основі CSV**  
  Редагуйте в Excel, Google Sheets або будь-якому редакторі CSV. За замовчуванням: роздільник `;`, UTF-8, текстовий кваліфікатор `"`.

- **12+ мов включено**  
  Англійська, китайська, японська, німецька, французька, іспанська, португальська, італійська, українська, польська, турецька, корейська. Додайте власні через enum `Language` або назву колонки.

- **Автоматична зміна шрифтів**  
  Призначте шрифти для кожної мови (наприклад, NotoSansTC для китайської), що автоматично підбираються при перекладі. Включає шрифти Noto Sans CJK + Audex з ліцензіями.

- **Динамічні плейсхолдери**  
  Вставляйте змінні в переклади: `{playerName}`, `{score}`, `{anyVariable}` → заповнюються під час виконання.

- **Підтримка тегів TextMeshPro**  
  Використовуйте `<color=#FF0000>`, `<sprite=0>`, емодзі 😊 безпосередньо в перекладах.

- **Два режими використання:**
  - **Статичний** – кешує текстові об'єкти, автоматично оновлює при зміні мови, включаючи зміну шрифту (ефективно для меню/UI)
  - **Динамічний** – просто зчитує значення, без вбудованої зміни шрифту (для рахунків/таймерів, кешовані пошуки)

<img width="1280" height="720" alt="Tooltip Localization System Unity" src="https://github.com/user-attachments/assets/8e773fe3-fff2-4990-a463-4fe290d7c7f1" />

## Швидкий старт

### 1. Налаштування

1. Імпортуйте пакет у ваш проєкт Unity
2. Знайдіть попередньо налаштований асет `InputData` у `Assets/CSV Localization/InputData`
3. Встановіть **Default Language** в інспекторі (англійська за замовчуванням)
4. Призначте ваші шрифти в інспекторі:
   - **Default Font** для мов на основі латиниці
   - **Language Special Fonts** для CJK мов (китайська, японська, корейська)
5. Додайте переклади до включеного файлу CSV (див. формат нижче)

> **Примітка:** Ви можете створити власний асет InputData (`Правий клік → Create → Localization System Mini → InputData`) та файл CSV за потреби.

### 2. Ініціалізація
```csharp
using LocalizationSystemMini;

public class GameManager : MonoBehaviour
{
    [SerializeField] private InputStringsScriptableObject textStrings;
    
    void Start()
    {
        // Ініціалізація системи локалізації (завантажує збережену мову або встановлює за замовчуванням)
        textStrings.InitializeLocalizator();
    }
}
```

### 3. Підключення до UI кнопок

В інспекторі → компонент Button → **OnClick()**:
   - Натисніть **+** щоб додати Event
   - Перетягніть scriptable object **InputData** в поле об'єкта
   - Виберіть **InputStringsScriptableObject → ChangeLanguage (string)**
   - Введіть назву мови в текстове поле: `English`, `Japanese`, `Ukrainian` тощо.

### 4. Використання в коді

> **Індексація рядків:** Візуальні редактори (Excel, Google Sheets) починаються з рядка 1, але код використовує індексацію від 0. Коли вказуєте номер рядка, віднімайте 1 від того, що бачите в редакторі (наприклад, рядок 2 в Excel = індекс 1 в коді). Альтернативно, використовуйте ключі замість номерів рядків. Це надійніше і дозволяє міняти рядки таблиці місцями.

**Статичний режим** – кешує текстові об'єкти, автоматично оновлює при зміні мови, керує зміною шрифту  
*Викликайте один раз: на Start або при створенні текстових об'єктів*
```csharp
void Start()
{
// Простий текст
textStrings.FillTextObject(2, textComponent);

// За ключем (шукає ключ в колонці A)
textStrings.FillTextObjectByKey("settings_key", textComponent);

// З плейсхолдерами (потребує повторного виклику для оновлення змінних)
textStrings.FillTextObjectWithPlaceholders(2, textComponent, userName, score);

textStrings.FillTextObjectWithPlaceholdersByKey("row_key", textComponent, userName, score);
}
```

**Динамічний режим** – зчитує на вимогу, без зміни шрифту  
*Викликайте в Update() або коли дані часто змінюються*
```csharp
void Update()
{
    // За індексом рядка
    textComponent.text = textStrings.GetStringText(2);
    
    // За ключем
    textComponent.text = textStrings.GetStringTextByKey("greeting_key");
    
    // З плейсхолдерами
    textComponent.text = textStrings.ReplacePlaceholders(3, playerName, level);

    textComponent.text = textStrings.ReplacePlaceholdersByKey("row_key", playerName, level);

    // Ручне оновлення шрифту (викликайте один раз, за потреби)
    textComponent.font = textStrings.GetCurrentFont();
}
```

**Зміна мови** – прив'яжіть до UI кнопок для перемикання мови
```csharp
// За enum
textStrings.ChangeLanguage(InputStringsScriptableObject.Language.Japanese);

// За рядком (рекомендовано для власних мов)
textStrings.ChangeLanguage("Ukrainian");
```

## Формат CSV

|   | <sub>A</sub> | <sub>B</sub>     | <sub>C</sub>     | <sub>D</sub>     | <sub>E</sub>     | <sub>F</sub>        | <sub>...</sub> |
|---|--------------|------------------|------------------|------------------|------------------|---------------------|----------------|
| <sub>1</sub> | Use keys     | Default          | English          | Chinese          | Japanese          | German              | ...            |
| <sub>2</sub> | greeting_key | hello            | hello            | 你好             | こんにちは         | hallo               | ...            |
| <sub>3</sub> | welcome_key  | Welcome, {name}! | Welcome, {name}! | 欢迎，{name}！    | ようこそ、{name}！ | Willkommen, {name}! | ...            |
| <sub>4</sub> | yes_key      | Yes              | Yes              | 是               | はい              | Ja                  | ...            |

- Перша колонка: унікальні ключі (для `GetStringTextByKey`)
- Перший рядок: назви мов для перемикання за назвою
- Друга колонка: резервні значення за замовчуванням (у повній версії Automatic Tutorial Maker тут зберігаються автогенеровані підказки туторіалу)
- Інші колонки: переклади мовами (порядок колонок = порядок enum Language)
- **Плейсхолдери:** Використовуйте синтаксис `{variableName}`. Позиція в реченні може відрізнятися між мовами, але **порядок має бути ідентичним** у всіх перекладах.

> **Важливо:** Плейсхолдери замінюються **за позицією, а не за назвою**. Система замінює 1-й плейсхолдер на 1-е значення, 2-й на 2-е значення тощо, незалежно від того, що всередині фігурних дужок.
> 
> **Приклад з викликом:** `ReplacePlaceholders(row, "John", 100)`
> 
> ✅ **Правильно:**  
> EN: `"Player {name} has {score} coins"` → "Player John has 100 coins"  
> UA: `"У гравця {name} є {score} монет"` → "У гравця John є 100 монет"  
> *(Однаковий порядок: {name} перший, {score} другий)*
> 
> ❌ **Неправильно:**  
> UA: `"{score} монет у гравця {name}"` → "John монет у гравця 100"
> *(Змінений порядок: система замінює {score} на "John", {name} на 100)*

## Керування шрифтами

Для мов зі спеціальними символами (китайська, японська, корейська):

1. В інспекторі InputData → **Language Special Fonts**:
   - Додайте назву мови: `Chinese`
   - Призначте TMP_FontAsset: `NotoSansTC-Regular SDF`
2. Встановіть **Default Font** для всіх інших мов

Система автоматично змінює шрифти на ті, що у списку, при зміні мови.

## Довідник API

### Методи

| Метод | Опис | Випадок використання |
|--------|-------------|----------|
| `InitializeLocalizator()` | Завантажити збережену мову або встановити за замовчуванням | Викликати один раз на старті гри |
| `ChangeLanguage(Language)` | Перемкнути на мову з enum | Тестування в інспекторі |
| `ChangeLanguage(string)` | Перемкнути за назвою рядка | UI кнопки мов, власні мови |
| `GetStringText(int row)` | Отримати текст за рядком | Динамічне зчитування |
| `GetStringTextByKey(string key)` | Отримати текст за ключем | Динамічне зчитування |
| `FillTextObject(int row, TMP_Text)` | Кешувати текстовий об'єкт | Статичний UI |
| `ReplacePlaceholders(int row, params object[])` | Отримати текст зі змінними | Динамічний зі змінними |
| `FillTextObjectWithPlaceholders(int row, TMP_Text, params object[])` | Кешувати текст зі змінними | Статичний зі змінними |
| `ReplacePlaceholdersByKey(string key, params object[])` | Отримати текст зі змінними | Динамічний зі змінними |
| `FillTextObjectWithPlaceholdersByKey(string key, TMP_Text textToFill, params object[])` | Кешувати текст зі змінними | Статичний зі змінними |
| `GetCurrentFont()` | Отримати шрифт поточної мови | Ручне керування шрифтом |

### Властивості

`_currentLanguageString` – Поточна мова як string. Відстежуйте це для виявлення змін мови в зовнішніх скриптах (наприклад, `if (language != textStrings._currentLanguageString)`).

<img width="1323" height="845" alt="Localization System Mini code" src="https://github.com/user-attachments/assets/411a6384-25c9-4385-9e16-7b6e5bd7f91c" />

## Приклад сцени

Відкрийте `DemoScene` щоб побачити всі можливості в дії.

[![Watch the video](https://github.com/user-attachments/assets/feb99d28-0808-4e02-a202-01e29587cefd)](https://youtu.be/0LdXRFOFWAw)

### Перемикання мов
- Кнопки з прапорами для перемикання між мовами
- Автоматична зміна шрифту для CJK мов

### Приклади динамічного режиму
- DynamicHelloText – простий динамічний текст (*спробуйте змінити поле "User Name" в Play Mode*)
- DynamicVarText – динамічний текст зі змінними (*спробуйте змінити поле "Dynamic Value" в Play Mode*)

### Приклади статичного режиму
- StaticTipText – статичний текст з автооновленням при зміні мови
- StaticVarText – статичний текст зі змінними (*спробуйте перемкнути мову щоб побачити автоматичне оновлення змінних*)

### Керування шрифтами
- Порівняйте автоматичну зміну шрифту (статичний режим) проти ручної (динамічний режим)
- Подивіться реалізацію в компоненті `TextCallExamples`

### Навчальні ресурси

Вивчіть `TextCallExamples.cs` для шаблонів реалізації:
- Як використовувати обидва - статичний та динамічний - режими
- Коли кешувати проти зчитування на вимогу
- Як вручну керувати зміною шрифту

## Примітки

- **Збережена мова**  
  Зберігається в `Application.persistentDataPath/LocalizationFolder/language_global.json`  
  Використовуйте кнопку **"Open Saved Language File"** в інспекторі InputData для навігації до цієї папки.

- **Продуктивність**  
  - Статичний режим: одноразове налаштування, кешування в пам'яті
  - Динамічний режим: покадрове зчитування з автоматичним кешуванням для частих звернень
  - Пошуки на основі словника оптимізують повторні зчитування

- **Власні мови**  
  Додайте нові мови:
  1. Додаючи колонки до файлу CSV
  2. Розширюючи enum `Language` в `InputStringsScriptableObject.cs`  
  АБО використовуйте `ChangeLanguage(string)` без модифікації enum

## Ліцензія

Безкоштовно для використання в особистих та комерційних проєктах.

## Підтримка

Для повнофункціональної версії чекніть: **[Automatic Tutorial Maker в Unity Asset Store](https://u3d.as/3tsL)**

Ця система використовується для локалізації текстових підказок, додатково може знаходити значення за найближчим співпадінням (без прив`язки до номера чи ключа рядка) та експортувати авто-згенеровані або вручну створені тексти у CSV зі Scriptable Object. Сховище містить усі вбудовані підказки, а також додаткові тексти, створені в усіх сценах і навчальних кроках Unity-проєкту.

[![YouTube](https://img.shields.io/badge/YouTube-Дивитись_Демо-red?style=for-the-badge&logo=youtube)](https://youtu.be/8RE4LOaLAI4)

<img width="1920" height="971" alt="ATM_full" src="https://github.com/user-attachments/assets/e8a628ed-81a9-4aeb-965d-dc6e36e8db07" />

P.S. На базі CSV вже є рішення Localization Package від Unity, яке містить більшість можливих функцій. А це - як проста версія для інді, з деякими своїми перевагами. Наприклад, вміє кешувати пару TMP_Text + його рядок, щоб при перекладі оновити всі пари автоматично, разом зі шрифтом. Є підтримка мов по будь-якій назві (можна вписати стовпчик “Elvish” - і по “Elvish” звертатись). І лише один ScriptableObject як "посередник", доступний звідусіль, не потрібно працювати з купою компонентів.

Розробник: https://www.octantastudio.com/

Gmail: octantastudio@gmail.com 

Discord: https://discord.gg/6SPxKpFZFC

[МОЖЛИВОСТІ](#можливості) | [ШВИДКИЙ СТАРТ](#швидкий-старт) | [ФОРМАТ CSV](#формат-csv) | [ШРИФТИ](#керування-шрифтами) | [API](#довідник-api) | [ДЕМО СЦЕНА](#приклад-сцени) | [ПРИМІТКИ](#примітки) | [ЛІЦЕНЗІЯ](#ліцензія) | [ПІДТРИМКА](#підтримка)
