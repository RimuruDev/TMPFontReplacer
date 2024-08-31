# TMPFontReplacer (TextMeshPro Font Replacer) для Unity

## Описание
TMP Font Replacer — это мощный инструмент для Unity Editor, предназначенный для автоматизации процесса замены шрифтов в компонентах TextMeshPro на всех префабах в указанной папке проекта. Этот инструмент идеально подходит для разработчиков и дизайнеров, которым необходимо быстро обновить шрифты в большом количестве префабов, экономя время и усилия.

## Ключевые особенности
- Поиск и замена шрифтов в компонентах TextMeshPro на всех префабах в указанной папке и её подпапках.
- Простой и понятный пользовательский интерфейс.
- Возможность выбора любого шрифта, импортированного в проект как `TMP_FontAsset`.

## Как использовать
1. **Подготовка:**
    - Убедитесь, что в вашем проекте Unity есть папка `Editor` на верхнем уровне вашего каталога `Assets`. Если такой папки нет, создайте её.
    - Скопируйте скрипт `TMPFontReplacer.cs` в папку `Editor`.
    - Или скачайте `TMPFontReplacer.unitypackage` в разлеле Release и просто перетащите в проект. Готово.

2. **Настройка и запуск:**
    - Откройте Unity и дождитесь компиляции скриптов :D
    - В меню `RimuruDev Tools` выберите `TMP Font Replacer`, чтобы открыть окно инструмента.
    - Введите путь к папке с префабами, где нужно заменить шрифты. Пример: `Assets/YourPrefabsFolder`.
    - Выберите новый шрифт для установки, используя поле выбора `New Font`.
    - Нажмите кнопку `Replace Fonts` для начала процесса замены шрифтов.

![image](https://github.com/RimuruDev/TMPFontReplacer/assets/85500556/22f91ae4-29c9-40be-a849-10e4f8e363a8)

![image](https://github.com/RimuruDev/TMPFontReplacer/assets/85500556/16fe4682-3806-4e53-ad90-01ec134533ab)

## Преимущества
Использование TMP Font Replacer позволяет значительно сократить время, необходимое для ручной замены шрифтов в большом количестве префабов, минимизируя рутинную работу и предотвращая ошибки. Это идеальное решение для проектов, в которых часто требуется обновление визуального стиля или исправление шрифтов.

## Важно знать
- LegacyFontReplacer для компонентов Text (Legacy). TMPFontReplacer для TextMeshPro.
- Этот инструмент предназначен для использования в редакторе Unity и не будет работать в сборке игры.
- Убедитесь, что выбранный шрифт уже импортирован в проект как `TMP_FontAsset`.
- Используйте инструмент с осторожностью, так как он перезаписывает существующие настройки шрифтов на префабах.
