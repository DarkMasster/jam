using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace Jam.Core.Localization.Editor
{
    public static class LocalizationSetup
    {
        private const string Root = "Assets/Game/Localization";
        private const string LocalesFolder = Root + "/Locales";
        private const string TablesFolder = Root + "/Tables";

        private readonly struct Entry
        {
            public Entry(string key, string ru, string en)
            {
                Key = key;
                Ru = ru;
                En = en;
            }

            public string Key { get; }
            public string Ru { get; }
            public string En { get; }
        }

        private static readonly Entry[] CommonEntries =
        {
            new("ui.main.title", "ОТРАЖЕНИЕ / ИМПУЛЬС", "REFLECTION / MOMENTUM"),
            new("ui.main.subtitle", "REFLECTION + MOMENTUM", "REFLECTION + MOMENTUM"),
            new("ui.main.new_game", "НАЧАТЬ НОВУЮ ИГРУ", "NEW GAME"),
            new("ui.main.continue", "ПРОДОЛЖИТЬ", "CONTINUE"),
            new("ui.main.quit", "ВЫХОД", "QUIT"),
            new("ui.main.footer", "ТРИ ИСТОРИИ • ОДИН МОМЕНТ", "THREE STORIES • ONE MOMENT"),
            new("ui.main.language", "ЯЗЫК: РУССКИЙ", "LANGUAGE: ENGLISH"),
            new("ui.main.no_save", "Сохранение не найдено", "No save found"),
            new("ui.main.last_scene", "Последняя точка: {0}", "Last checkpoint: {0}"),
            new("ui.main.loading", "Загрузка…", "Loading…"),
            new("ui.main.error.no_start", "Не найдена сцена начала игры.", "Could not find the starting scene."),
            new("ui.main.error.invalid_save", "Сохранение не найдено или устарело.", "The save is missing or outdated."),
            new("ui.character.title", "ВЫБЕРИТЕ ИСТОРИЮ", "CHOOSE A STORY"),
            new("ui.character.subtitle", "ТРИ МАРШРУТА • ОДИН ДЕНЬ", "THREE ROUTES • ONE DAY"),
            new("ui.character.progress", "ПРОЙДЕНО ИСТОРИЙ: {0} / 3", "STORIES COMPLETED: {0} / 3"),
            new("ui.character.drive.route", "МОСКВА → ТБИЛИСИ", "MOSCOW → TBILISI"),
            new("ui.character.drive.description", "РУКОВОДИТЕЛЬ • ДОРОГА", "EXECUTIVE • THE ROAD"),
            new("ui.character.office.route", "ЕКАТЕРИНБУРГ → АЛМАТЫ", "YEKATERINBURG → ALMATY"),
            new("ui.character.office.description", "РАЗРАБОТЧИК • ОФИСНЫЙ КОШМАР", "DEVELOPER • OFFICE NIGHTMARE"),
            new("ui.character.photo.route", "САНКТ-ПЕТЕРБУРГ → БАЛИ", "SAINT PETERSBURG → BALI"),
            new("ui.character.photo.description", "ФОТОГРАФКА • ЧЕСТНЫЙ КАДР", "PHOTOGRAPHER • AN HONEST SHOT"),
            new("ui.character.completed", "✓ ПРОЙДЕНО", "✓ COMPLETED"),
            new("ui.character.current", "• ТЕКУЩАЯ", "• CURRENT"),
            new("ui.character.finale.locked", "ФИНАЛ • ЗАБЛОКИРОВАН", "FINALE • LOCKED"),
            new("ui.character.finale.unlocked", "ФИНАЛ • РАЗБЛОКИРОВАН", "FINALE • UNLOCKED"),
            new("ui.character.back", "НАЗАД В ГЛАВНОЕ МЕНЮ", "BACK TO MAIN MENU"),
            new("ui.character.status.default", "Истории можно проходить в любом порядке. Прогресс сохраняется автоматически.", "Stories can be played in any order. Progress is saved automatically."),
            new("ui.character.status.unlocked", "Все три линии завершены. Финал разблокирован.", "All three stories are complete. The finale is unlocked."),
            new("ui.character.status.loading", "Сохранение… Загрузка…", "Saving… Loading…"),
            new("ui.character.error.no_finale", "Финал разблокирован, но сцена Finale ещё не добавлена в Build Settings.", "The finale is unlocked, but the Finale scene is not in Build Settings yet."),
            new("ui.character.error.no_scene", "Для линии {0} не найдена игровая сцена.", "No gameplay scene was found for story {0}."),
            new("ui.hud.menu", "МЕНЮ  [ESC]", "MENU  [ESC]"),
            new("ui.hud.pause.title", "МЕНЮ", "MENU"),
            new("ui.hud.pause.subtitle", "ИГРА ПРИОСТАНОВЛЕНА", "GAME PAUSED"),
            new("ui.hud.resume", "ПРОДОЛЖИТЬ", "RESUME"),
            new("ui.hud.save", "СОХРАНИТЬ", "SAVE"),
            new("ui.hud.exit", "ВЫЙТИ В ГЛАВНОЕ МЕНЮ", "EXIT TO MAIN MENU"),
            new("ui.hud.save.available", "Сохранение запишет состояние текущего режима.", "Save the current game mode state."),
            new("ui.hud.save.unavailable", "Этот режим пока не поддерживает ручное сохранение.", "This mode does not support manual saving yet."),
            new("ui.cutscene.progress", "{0} / {1}   •   ПРОБЕЛ / КЛИК", "{0} / {1}   •   SPACE / CLICK"),
            new("ui.cutscene.skip", "ПРОПУСТИТЬ  [ESC]", "SKIP  [ESC]")
        };

        private static readonly Entry[] PhotoEntries =
        {
            new("prologue.intro.001.speaker", "ОНА", "HER"),
            new("prologue.intro.001.text", "24 февраля 2022 года. Утро начинается с новостей, которым не находится места в привычной жизни.", "February 24, 2022. The morning begins with news that no longer fits into ordinary life."),
            new("prologue.intro.002.speaker", "РЕДАКТОР", "EDITOR"),
            new("prologue.intro.002.text", "Агентство закрывает российский офис. Проекты остановлены. Команда распущена.", "The agency is closing its Russian office. Projects are frozen. The team is dismissed."),
            new("prologue.intro.003.speaker", "ТЕЛЕФОН", "PHONE"),
            new("prologue.intro.003.text", "Forbidgram недоступен. Клиенты молчат. В телефоне остаются архив, незакрытые счета и билет в один конец.", "Forbidgram is unavailable. Clients are silent. Her phone holds an archive, unpaid invoices, and a one-way ticket."),
            new("prologue.intro.004.speaker", "ОНА", "HER"),
            new("prologue.intro.004.text", "Перед отъездом нужен ещё один кадр — достаточно честный, чтобы не предать себя, и достаточно заметный, чтобы оплатить дорогу.", "Before leaving, she needs one last photograph—honest enough not to betray herself, visible enough to pay for the journey."),
            new("mode.name", "История фотографки", "Photographer's story"),
            new("phase.intro", "ПРОЛОГ • САНКТ-ПЕТЕРБУРГ", "PROLOGUE • SAINT PETERSBURG"),
            new("phase.explore", "ИССЛЕДОВАНИЕ • ДВОР И ПОЧТОВЫЕ ЯЩИКИ", "EXPLORE • COURTYARD AND MAILBOXES"),
            new("phase.camera", "КАМЕРА • ВЫБОР КАДРА", "CAMERA • CHOOSE THE SHOT"),
            new("phase.publish", "FORBIDGRAM • ПУБЛИКАЦИЯ", "FORBIDGRAM • PUBLISH"),
            new("phase.reflection", "ОТРАЖЕНИЕ", "REFLECTION"),
            new("phase.arrival", "ДУБАЙ • ТРАНЗИТНАЯ ГОСТИНИЦА", "DUBAI • TRANSIT HOTEL"),
            new("speaker.whitebox", "WHITE-BOX СЦЕНА", "WHITE-BOX SCENE"),
            new("speaker.viewfinder", "ВИДОИСКАТЕЛЬ", "VIEWFINDER"),
            new("speaker.honest_shot", "ЧЕСТНЫЙ КАДР", "HONEST SHOT"),
            new("speaker.safe_shot", "БЕЗОПАСНЫЙ КАДР", "SAFE SHOT"),
            new("speaker.prologue_finale", "WHITE-BOX ФИНАЛ ПРОЛОГА", "WHITE-BOX PROLOGUE FINALE"),
            new("status.continue", "Продолжение: {0}", "Continue: {0}"),
            new("status.exposition", "Экспозиция {0} / {1}", "Exposition {0} / {1}"),
            new("status.inspected", "Осмотрено: {0} / 3", "Inspected: {0} / 3"),
            new("status.no_target", "Цель не выбрана", "No target selected"),
            new("status.focus", "Фокус: {0}", "Focus: {0}"),
            new("status.published", "Truth +{0}   •   Reach +{1}   •   Платёж получен", "Truth +{0}   •   Reach +{1}   •   Payment received"),
            new("status.saved_choice", "Сохранённый выбор: {0} • Truth {1} • Reach {2}", "Saved choice: {0} • Truth {1} • Reach {2}"),
            new("status.arrival_saved", "Checkpoint photo.arrival сохранён", "Checkpoint photo.arrival saved"),
            new("status.saved", "Сохранено: {0}", "Saved: {0}"),
            new("action.next", "ДАЛЕЕ", "NEXT"),
            new("action.enter_courtyard", "ВЫЙТИ К ПОДЪЕЗДУ", "ENTER THE COURTYARD"),
            new("action.take_camera", "ДОСТАТЬ КАМЕРУ", "TAKE OUT CAMERA"),
            new("action.summons", "[ ПОВЕСТКА ] • Truth", "[ DRAFT NOTICE ] • Truth"),
            new("action.butterfly", "[ БАБОЧКА ] • Reach", "[ BUTTERFLY ] • Reach"),
            new("action.shutter", "СПУСК ЗАТВОРА", "RELEASE SHUTTER"),
            new("action.reflect", "ПОСМОТРЕТЬ НА СВОЙ ВЫБОР", "FACE YOUR CHOICE"),
            new("action.airport", "ЕХАТЬ В АЭРОПОРТ", "GO TO THE AIRPORT"),
            new("action.complete", "ЗАВЕРШИТЬ ПРОЛОГ", "COMPLETE PROLOGUE"),
            new("explore.description", "Серый подъезд. У стены — почтовые ящики. Рядом лежит собранный чемодан. Телефон продолжает вибрировать. Осмотрите три детали, чтобы разблокировать камеру.", "A grey entrance hall. Mailboxes line the wall; a packed suitcase waits nearby. The phone keeps vibrating. Inspect three details to unlock the camera."),
            new("inspect.phone.label", "ТЕЛЕФОН • сообщение редактора", "PHONE • editor's message"),
            new("inspect.phone.text", "Агентство уезжает. Зарплаты за последний месяц может не быть.", "The agency is leaving. Last month's salary may never arrive."),
            new("inspect.suitcase.label", "ЧЕМОДАН • билет через Дубай", "SUITCASE • ticket via Dubai"),
            new("inspect.suitcase.text", "Маршрут заканчивается словом «Бали». Дальше — пустое место.", "The route ends with the word ‘Bali’. Beyond it is blank space."),
            new("inspect.mailbox.label", "ПОЧТОВЫЙ ЯЩИК • движение внутри", "MAILBOX • movement inside"),
            new("inspect.mailbox.text", "Из щели торчит военная повестка. На холодном металле садится бабочка.", "A military draft notice protrudes from the slot. A butterfly lands on the cold metal."),
            new("camera.summons", "В рамке — край почтового ящика и торчащая повестка. Это честно, но небезопасно.", "The frame catches the mailbox edge and the protruding draft notice. Honest, but unsafe."),
            new("camera.butterfly", "В рамке — бабочка на металле. Красиво, безопасно и почти ничего не говорит о происходящем.", "The frame holds a butterfly on metal. Beautiful, safe, and almost silent about what is happening."),
            new("camera.none", "Обе цели находятся в одной композиции. Выберите, на чём сфокусировать кадр.", "Both subjects share one composition. Choose where to place the focus."),
            new("publish.summons", "Публикацию замечают быстро. Вместе с поддержкой приходят вопросы и страх удалить снимок.", "The post gets noticed quickly. Support arrives with questions—and the fear of deleting the photograph."),
            new("publish.butterfly", "Лайки растут быстрее обычного. Никто не спрашивает, что находилось в нескольких сантиметрах от бабочки.", "Likes climb faster than usual. Nobody asks what lay only centimetres from the butterfly."),
            new("reflection.summons", "Я хотя бы не сделала вид, что ничего не происходит. Теперь надо решить, сколько правды я смогу увезти с собой.", "At least I did not pretend nothing was happening. Now I must decide how much truth I can carry with me."),
            new("reflection.butterfly", "Красивый кадр снова сработал. Только почему он ощущается ещё одной вещью, которую я оставляю здесь?", "The beautiful shot worked again. Why does it feel like one more thing I am leaving behind?"),
            new("arrival.description", "Дверь гостиничного номера закрывается. На экране телефона остаётся опубликованный кадр. Маршрут продолжается на Бали — уже в следующем акте.", "The hotel-room door closes. The published photograph remains on the phone screen. The route continues to Bali in the next act."),
            new("choice.none", "Не выбрано", "None"),
            new("choice.summons", "Повестка", "Draft notice"),
            new("choice.butterfly", "Бабочка", "Butterfly")
        };

        private static readonly Entry[] OfficeEntries =
        {
            new("hud.empty_hands", "РУКИ СВОБОДНЫ • ПОДОЙДИ К ПОДСВЕЧЕННОМУ ПРЕДМЕТУ", "HANDS FREE • APPROACH A HIGHLIGHTED ITEM"),
            new("zone.start", "СТАРТОВЫЙ КАБИНЕТ", "STARTING OFFICE"),
            new("objective.collect", "СОБЕРИ ЛИЧНЫЕ ВЕЩИ   {0}   {1}", "COLLECT PERSONAL ITEMS   {0}   {1}"),
            new("objective.laptop.empty", "[ ] НОУТБУК", "[ ] LAPTOP"),
            new("objective.laptop.done", "[X] НОУТБУК", "[X] LAPTOP"),
            new("objective.mug.empty", "[ ] КРУЖКА", "[ ] MUG"),
            new("objective.mug.done", "[X] КРУЖКА", "[X] MUG"),
            new("momentum.idle", "ПРОСТОЙ", "IDLE"),
            new("momentum.moving", "В ДВИЖЕНИИ", "MOVING"),
            new("momentum.label", "ТЕМП {0}%   {1}", "MOMENTUM {0}%   {1}"),
            new("item.keyboard", "КЛАВИАТУРА", "KEYBOARD"),
            new("item.printer", "ПРИНТЕР", "PRINTER"),
            new("enemy.chair", "ОФИСНОЕ КРЕСЛО", "OFFICE CHAIR"),
            new("hud.start_hint", "WASD / СТРЕЛКИ • ДВИГАЙСЯ К EXIT", "WASD / ARROWS • MOVE TOWARD EXIT"),
            new("hud.carrying", "В РУКАХ: {0} • PRIMARY — БРОСОК", "CARRYING: {0} • PRIMARY — THROW"),
            new("hud.integrity", "РАБОТОСПОСОБНОСТЬ {0}   ПОПЫТКА {1}", "CAPACITY {0}   ATTEMPT {1}"),
            new("hud.down", "ПРОИЗВОДИТЕЛЬНОСТЬ НЕУДОВЛЕТВОРИТЕЛЬНА\nПОПЫТКА {0} НАЧИНАЕТСЯ", "PERFORMANCE UNSATISFACTORY\nATTEMPT {0} IS STARTING"),
            new("status.laptop_collected", "НОУТБУК СОБРАН • ОСТАЛАСЬ КРУЖКА", "LAPTOP COLLECTED • MUG REMAINS"),
            new("status.mug_collected", "КРУЖКА СОБРАНА • ОСТАЛСЯ НОУТБУК", "MUG COLLECTED • LAPTOP REMAINS"),
            new("status.items_collected", "ЛИЧНЫЕ ВЕЩИ СОБРАНЫ • ДОБЕРИСЬ ДО EXIT", "PERSONAL ITEMS COLLECTED • REACH EXIT"),
            new("status.destroyed", "РАЗРУШЕНО: {0} • {1}/{2} • ТЕМП РАСТЁТ", "DESTROYED: {0} • {1}/{2} • MOMENTUM RISING"),
            new("status.wrecked", "СПИСАНО: {0} • {1}/{2} • ТЕМП РАСТЁТ", "WRITTEN OFF: {0} • {1}/{2} • MOMENTUM RISING"),
            new("status.chaser_missed", "{0} ПРОМАХНУЛОСЬ • ОКНО ДЛЯ БРОСКА", "{0} MISSED • THROW WINDOW OPEN"),
            new("status.picked_up", "ПОДОБРАНО: {0} • РУКИ ЗАНЯТЫ", "PICKED UP: {0} • HANDS OCCUPIED"),
            new("status.thrown", "БРОСОК: {0}", "THROWN: {0}"),
            new("status.hit", "УДАР: {0} • РАБОТОСПОСОБНОСТЬ {1}/{2}", "HIT: {0} • CAPACITY {1}/{2}"),
            new("status.failed", "ВЫГОРАНИЕ • ПРИЧИНА: {0}", "BURNOUT • CAUSE: {0}"),
            new("status.restarted", "РАБОЧИЙ ДЕНЬ ПЕРЕЗАПУЩЕН • ПОПЫТКА {0}", "WORKDAY RESTARTED • ATTEMPT {0}"),
            new("status.missing_both", "НУЖНЫ НОУТБУК И КРУЖКА", "LAPTOP AND MUG REQUIRED"),
            new("status.missing_laptop", "НУЖЕН НОУТБУК", "LAPTOP REQUIRED"),
            new("status.missing_mug", "НУЖНА КРУЖКА", "MUG REQUIRED"),
            new("status.access_denied", "ДОСТУП ОТКЛОНЁН • {0}", "ACCESS DENIED • {0}"),
            new("objective.false_exit", "EXIT — ЛОЖНАЯ ЦЕЛЬ • ВПЕРЕДИ СЕРВЕРНЫЙ БОСС", "EXIT IS A FALSE GOAL • SERVER BOSS AHEAD"),
            new("status.access_revoked", "ДОСТУП ОТОЗВАН • ЗОНА БОССА ГОТОВА ДЛЯ СЛЕДУЮЩЕГО СРЕЗА", "ACCESS REVOKED • BOSS ZONE READY FOR THE NEXT SLICE")
        };

        [MenuItem("Jam/Localization/Create or Update Localization")]
        public static void CreateOrUpdate()
        {
            EnsureFolder(Root);
            EnsureFolder(LocalesFolder);
            EnsureFolder(TablesFolder);

            var russian = EnsureLocale("ru", "Russian");
            var english = EnsureLocale("en", "English");
            var pseudo = EnsurePseudoLocale();
            var locales = new List<Locale> { russian, english, pseudo };

            EnsureTable(LocalizationTables.Common, CommonEntries, locales, russian, english, pseudo);
            EnsureTable(LocalizationTables.Photo, PhotoEntries, locales, russian, english, pseudo);
            EnsureTable(LocalizationTables.Office, OfficeEntries, locales, russian, english, pseudo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Localization ready: ru, en and qps-ploc; Common, Photo and Office tables updated.");
        }

        private static Locale EnsureLocale(string code, string displayName)
        {
            var identifier = new LocaleIdentifier(code);
            var locale = LocalizationEditorSettings.GetLocale(identifier);
            if (locale != null)
            {
                return locale;
            }

            locale = Locale.CreateLocale(code);
            locale.name = displayName;
            AssetDatabase.CreateAsset(locale, $"{LocalesFolder}/{code}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static Locale EnsurePseudoLocale()
        {
            var identifier = new LocaleIdentifier("qps-ploc");
            var locale = LocalizationEditorSettings.GetLocale(identifier);
            if (locale != null)
            {
                locale.name = "qps-ploc";
                EditorUtility.SetDirty(locale);
                return locale;
            }

            var assetPath = $"{LocalesFolder}/qps-ploc.asset";
            var pseudo = AssetDatabase.LoadAssetAtPath<PseudoLocale>(assetPath);
            if (pseudo != null)
            {
                LocalizationEditorSettings.RemoveLocale(pseudo);
            }
            else
            {
                pseudo = PseudoLocale.CreatePseudoLocale();
                AssetDatabase.CreateAsset(pseudo, assetPath);
            }

            pseudo.Identifier = identifier;
            pseudo.name = "qps-ploc";
            EditorUtility.SetDirty(pseudo);
            LocalizationEditorSettings.AddLocale(pseudo);
            return pseudo;
        }

        private static void EnsureTable(
            string tableName,
            IReadOnlyList<Entry> entries,
            IList<Locale> locales,
            Locale russian,
            Locale english,
            Locale pseudo)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName)
                             ?? LocalizationEditorSettings.CreateStringTableCollection(tableName, TablesFolder, locales);

            UpdateTable((StringTable)collection.GetTable(russian.Identifier), entries, static entry => entry.Ru);
            UpdateTable((StringTable)collection.GetTable(english.Identifier), entries, static entry => entry.En);
            UpdateTable((StringTable)collection.GetTable(pseudo.Identifier), entries, static entry => entry.Ru);
            EditorUtility.SetDirty(collection.SharedData);
        }

        private static void UpdateTable(
            StringTable table,
            IReadOnlyList<Entry> entries,
            Func<Entry, string> valueSelector)
        {
            foreach (var source in entries)
            {
                var entry = table.GetEntry(source.Key) ?? table.AddEntry(source.Key, valueSelector(source));
                entry.Value = valueSelector(source);
            }

            EditorUtility.SetDirty(table);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var separator = path.LastIndexOf('/');
            var parent = path[..separator];
            var name = path[(separator + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
