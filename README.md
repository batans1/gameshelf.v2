ТОВА Е GAMESHELF NO.2

Проектът беше надграден от платформа за оферти до система с по-добра сигурност, модерация на съдържание, потребителски профили и „savings cart“ (количка за спестявания).

1) Стабилност и сигурност

Файл: `Gameshelf.Web/Program.cs`

- Премахната е дублирана Serilog конфигурация, която причиняваше двойни логове.
- Добавена е проверка за `DefaultConnection` (липсваща/placeholder стойност) при старт.
- Swagger е изключен извън development.
- Добавен е глобален rate limiter (освен съществуващите policy лимити).
- Добавен е именуван `HttpClient` за CheapShark с timeout и User-Agent.

2) По-надежден sync на live deals

Файл: `Gameshelf.Business/Services/Implementations/LiveDealSyncService.cs`

- Прехвърлен към именуван HTTP клиент.
- Подобрено страниране и диагностика при API грешки.
- Логовете вече включват отрязано съдържание на грешния response.

3) Нова система за модерация на ревюта

Нови файлове:
- `Gameshelf.Models/Domain/Entities/UserModerationStatus.cs`
- `Gameshelf.Business/Services/Interfaces/IReviewModerationService.cs`
- `Gameshelf.Business/Services/Moderation/ModerationOutcome.cs`
- `Gameshelf.Business/Services/Moderation/ReviewModerationException.cs`
- `Gameshelf.Business/Services/Implementations/ReviewModerationService.cs`

Какво добавя:
- Засичане + цензуриране на profanity.
- Ескалация:
  - 6 предупреждения -> 30 мин timeout (1-ви strike)
  - още 6 -> 6 часа timeout (2-ри strike)
  - още 6 -> 24 часа timeout (3-ти strike)
  - след 3-ти strike: всяка profanity дума -> директен 24ч timeout
- Върща съобщение към потребителя в изискания формат.
- Поправен е concurrency проблемът при първо създаване на moderation статус.
- Разширен е списъкът с profanity думи и вариации.

4) Интеграция в rating service и API

Променени:
- `Gameshelf.Business/Services/Interfaces/IDealRatingService.cs`
- `Gameshelf.Business/Services/Implementations/DealRatingService.cs`
- `Gameshelf.Web/Controllers/Api/DealRatingsApiController.cs`
- `Gameshelf.Web/Views/Platforms/GameDetails.cshtml`

Добавено:
- `SetRatingAsync` връща moderation резултат.
- API връща warning при profanity.
- Добавен admin endpoint за премахване само на текстовото ревю:
  - `DELETE /api/deal-ratings/{ratingId}/review-text`
- В UI:
  - warning съобщения при profanity
  - admin бутон за премахване на текст
  - username в ревютата става кликаем към публичен профил

5) Savings cart (нова функционалност)

Нови файлове:
- `Gameshelf.Models/Domain/Entities/SavingsCartItem.cs`
- `Gameshelf.Models/ViewModels/Profile/SavingsCartItemViewModel.cs`
- `Gameshelf.Models/ViewModels/Profile/SavingsCartSummaryViewModel.cs`
- `Gameshelf.Business/Services/Interfaces/ISavingsCartService.cs`
- `Gameshelf.Business/Services/Implementations/SavingsCartService.cs`
- `Gameshelf.Web/Controllers/Api/SavingsCartApiController.cs`

Какво добавя:
- Добавяне/махане от количката.
- Обобщение:
  - обща цена на офертите
  - обща оригинална цена
  - общо спестяване
- API:
  - `GET /api/savings-cart`
  - `POST /api/savings-cart/{gameDealId}`
  - `DELETE /api/savings-cart/{gameDealId}`
- В профила:
  - таблица със спестявания
  - кликаеми имена на игри към съответния saved deal

6) Нова структура на профили (public/private)

Нови:
- `Gameshelf.Models/ViewModels/Profile/PublicProfileViewModel.cs`
- `Gameshelf.Web/Controllers/ProfileController.cs`
- `Gameshelf.Web/Views/Profile/Me.cshtml`
- `Gameshelf.Web/Views/Profile/Public.cshtml`

Маршрути:
- private: `GET /profiles/me`
- public: `GET /profile/{username}`

В `_LoginPartial` линкът „Hello ...“ вече води към `/profiles/me`.

7) Avatar + username управление

Нови:
- `Gameshelf.Models/Domain/Entities/UserProfile.cs`
- конфигурация в `ApplicationDbContext`

Добавено:
- смяна на username (валидация + проверка за уникалност)
- качване на avatar изображение
- съхранение на avatar path в БД
- бутон за премахване на avatar:
  - `POST /profiles/me/remove-avatar`

8) Подобрения по dark mode

Файл: `Gameshelf.Web/wwwroot/css/site.css`

- оправен някой текст, който не се виждаше добре в дарк моуд

9) Подсилена авторизация

Файл: `Gameshelf.Web/Controllers/Api/DealClicksApiController.cs`

- Добавена е ресурсна проверка с `PlatformAccessPolicy` за click stats.
- Ограничен е достъпът до чужди платформи само по име.

10) Privacy страница

Добавен:
- `Gameshelf.Web/Views/Home/Privacy.cshtml`

---> няма exception от линка към `Home/Privacy`.

ефекта е

- По-добра безопасност и контрол на съдържанието.
- По-силна защита и по-малка повърхност за злоупотреби.
- По-добро потребителско преживяване (профил, avatar, username, savings cart).
- По-добра прозрачност за спестяванията и връщане към запазените оферти.
- По-добра четимост и достъпност в dark mode.
