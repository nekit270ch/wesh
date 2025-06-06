# Уведомление
Это архивный репозиторий, содержащий версии wesh 0.3 - 2.7. В настоящее время ведётся разработка версий 3.x в [этом репозитории](https://github.com/ryzhpolsos/wesh). Новые версии не совместимы со старыми.

# wesh
wesh - cкриптовый язык для Windows, созданный для упрощения работы с WinAPI и автоматизации системы.

## Установка
Помимо обычного способа установки, описанного [в документации](https://nekit270ch.github.io/wesh/#%D1%81%D0%BA%D0%B0%D1%87%D0%B8%D0%B2%D0%B0%D0%BD%D0%B8%D0%B5-%D0%B8-%D1%83%D1%81%D1%82%D0%B0%D0%BD%D0%BE%D0%B2%D0%BA%D0%B0), wesh можно установить одной командой:

`curl -L https://nekit270.ch/getwesh -o wesh.exe && wesh -c "load setup"`

Примечание: командную строку необходимо запустить с правами администратора.

Если необходима 32-битная версия wesh, вместо `https://nekit270.ch/getwesh` используйте `https://nekit270.ch/getwesh86`.

## Документация
Документация wesh доступна на [GitHub Pages](https://nekit270ch.github.io/wesh).

## Сборка
Сборка wesh осуществляется специальным скриптом `build.bat`. Синтаксис скрипта:

`build`  
Выполнить сборку с настройками по умолчанию.

`build /only64`  
Собрать только 64-битную версию EXE.

`build /only86`  
Собрать только 32-битную версию EXE.

Прочие параметры:
- `/o <путь к файлу>`  
  Поместить EXE по указанному пути.
- `/s <путь к скрипту>`  
  Внедрить указанный скрипт в ресурсы. После запуска wesh вместо обработки командной строки или запуска REPL будет выполнен код этого скрипта.
