using System.Collections.Generic;
namespace ClearWord.Services
{
    public class LanguageService
    {
        private static LanguageService? _i;
        public static LanguageService I => _i ??= new LanguageService();
        private string _l = "en-US";
        public string L { get => _l; set => _l = value; }

        private Dictionary<string, Dictionary<string, string>> _d = new()
        {
            ["en-US"] = new()
            {
                ["File"]="_File",["New"]="_New",["Open"]="_Open...",["Save"]="_Save",["SaveAs"]="Save _As...",["Exit"]="E_xit",
                ["Edit"]="_Edit",["Undo"]="_Undo",["Redo"]="_Redo",["Cut"]="Cu_t",["Copy"]="_Copy",["Paste"]="_Paste",["SelectAll"]="Select _All",
                ["Settings"]="_Settings",["SpellCheck"]="Spell Check",["GitHub"]="Our GitHub",
                ["Language"]="_Language",["English"]="English",["Russian"]="Русский",
                ["Bold"]="Bold",["Italic"]="Italic",["Underline"]="Underline",
                ["FontColor"]="Text Color",["BgColor"]="Canvas Color",
                ["SavePrompt"]="Save changes?",["OpenTitle"]="Open Document",["SaveTitle"]="Save As",
                ["Untitled"]="Untitled",["Words"]="Words: {0}",["LangStatus"]="English"
            },
            ["ru-RU"] = new()
            {
                ["File"]="_Файл",["New"]="_Новый",["Open"]="_Открыть...",["Save"]="_Сохранить",["SaveAs"]="Сохранить _как...",["Exit"]="В_ыход",
                ["Edit"]="_Правка",["Undo"]="_Отменить",["Redo"]="_Повторить",["Cut"]="Вы_резать",["Copy"]="_Копировать",["Paste"]="_Вставить",["SelectAll"]="Выделить _всё",
                ["Settings"]="_Настройки",["SpellCheck"]="Проверка орфографии",["GitHub"]="Наш GitHub",
                ["Language"]="_Язык",["English"]="English",["Russian"]="Русский",
                ["Bold"]="Жирный",["Italic"]="Курсив",["Underline"]="Подчёркнутый",
                ["FontColor"]="Цвет текста",["BgColor"]="Цвет холста",
                ["SavePrompt"]="Сохранить изменения?",["OpenTitle"]="Открыть документ",["SaveTitle"]="Сохранить как",
                ["Untitled"]="Безымянный",["Words"]="Слов: {0}",["LangStatus"]="Русский"
            }
        };

        public string S(string k) => _d[_l].TryGetValue(k, out var v) ? v : k;
    }
}