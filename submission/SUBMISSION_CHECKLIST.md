# Сабмит UstazAI — чек-лист (дедлайн 19 сентября, 12:00, Астана)

Подаёт капитан на aistartify.com, поле «Промокод / код участия»: `LOCUSCASE2`.

## 0. Безопасность (сделать первым)
- [ ] Отозвать старый ключ Gemini в aistudio.google.com и создать новый (старый был в `appsettings.json`).
- [ ] Придумать новый JWT-секрет (32+ символа). Старый `SUPER_SECRET_...` считать скомпрометированным.
- [ ] Новые значения только в `UstazAI/UstazAI/appsettings.Development.json` (уже в `.gitignore`) и в переменных окружения хостинга.
- [ ] Проверить, что репозиторий не содержит секретов: `git grep -n "AIza\|AQ\.\|SigningKey"`.

## 1. GitHub (обязательно, история проверяется)
Один репозиторий на обе папки (корень `LocusHackathon v2`). Создайте пустой публичный репозиторий на github.com, затем:
```bash
cd "LocusHackathon v2"
git init -b main
git add .
git status            # убедиться: нет appsettings.Development.json, .env.local, node_modules
git commit -m "UstazAI: final submission"
git remote add origin https://github.com/<логин>/<репозиторий>.git
git push -u origin main
```
Историю не подделывайте и даты не меняйте. Финальная версия должна быть в `main` до 12:00.

## 2. Бесплатный деплой
| Часть | Сервис | Как |
|---|---|---|
| Frontend | Vercel (Hobby) | Import репозитория, Root Directory `UztazAIDesktop`, переменная `NEXT_PUBLIC_API_BASE_URL=https://<бэкенд>` |
| Backend | Render (Free Web Service, Docker) | Root Directory `UstazAI`, Dockerfile; переменные ниже |
| База | Azure SQL (бесплатный тариф) | Строка подключения в `ConnectionStrings__Default` |

Переменные окружения backend:
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=<строка Azure SQL>
Jwt__SigningKey=<новый секрет>
Gemini__ApiKey=<новый ключ>
Gemini__ExtraApiKeys__0=<второй ключ, желательно из другого проекта>   (по желанию)
```
- Миграции и демо-каталог применяются при старте (`DbSeeder`), судейский аккаунт создаётся сам.
- Free-сервис Render засыпает через 15 минут: настройте бесплатный UptimeRobot на `https://<бэкенд>/health` раз в 5 минут и **откройте сайт за 2 минуты до показа**.
- CORS: в `UstazAI/UstazAI/Program.cs` (строка с `AllowAnyOrigin`) замените на домен Vercel, если успеете.
- Ссылки должны работать без оплаты до объявления результатов.

## 3. Проверка на проде (10 минут)
- [ ] Вход `judge@ustazai.demo` / `JudgePass123!`
- [ ] Анкета → диагностика → 3+ рекомендации → сравнение (карта) → roadmap → следующий шаг
- [ ] Смена бюджета/страны меняет рекомендации
- [ ] Мобильный экран (телефон или DevTools)
- [ ] Переключение ru/kk/en
- [ ] Заранее прогнать «Проверить через интернет» для 3–5 программ (сохраняется в базе), пока не кончилась квота поиска

## 4. Материалы для формы
- [ ] Ссылка на сайт + тестовый логин/пароль (см. выше)
- [ ] Ссылка на GitHub
- [ ] README: корневой `README.md` (задача, решение, стек, архитектура, запуск, тестовый сценарий, роли, источники, AI/API, готовые компоненты, ограничения). Заполните раздел «Команда и роли» и ссылки в таблице сверху
- [ ] Демо-видео до 3 минут (сценарий: `DEMO_VIDEO_SCRIPT.md`)
- [ ] Презентация PDF до 8 слайдов: `UstazAI_presentation.pdf`. Замените заполнители `<ССЫЛКА-НА-САЙТ>` и `<Имя>` в `slides.html` и добавьте скриншоты, затем пересоберите PDF (команда внизу)
- [ ] Техническая справка: раздел «Технологии» и «Источники данных и AI/API» корневого README

Пересборка PDF после правок `slides.html`:
```bash
"/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe" --headless --no-pdf-header-footer "--print-to-pdf=C:\путь\UstazAI_presentation.pdf" "file:///C:/путь/slides.html"
```

## 5. Финальная защита 24 сентября (Google Meet)
5 минут показ и живая демо на **той же версии**, что и на дедлайн, потом 5 минут вопросов. Возможные вопросы: как считаются шансы (детерминированный движок, AI только объясняет), откуда данные (демо с пометкой, проверка через интернет с источниками), что если AI недоступен (резервные тексты и бейдж), как защищаетесь от «гарантий поступления» (guardrails).
