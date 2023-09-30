# wesh
wesh - расширяемая оболочка командной строки для Windows с простым синтаксисом

## Установка
Помимо обычного способа установки, описанного [в документации](https://nekit270ch.github.io/wesh/#%D1%81%D0%BA%D0%B0%D1%87%D0%B8%D0%B2%D0%B0%D0%BD%D0%B8%D0%B5-%D0%B8-%D1%83%D1%81%D1%82%D0%B0%D0%BD%D0%BE%D0%B2%D0%BA%D0%B0), wesh можно установить одной командой:

`curl -L https://nekit270.ch/getwesh -o wesh.exe && wesh -c "load setup"`

Примечание: командную строку необходимо запустить с правами администратора.

Если необходима 32-битная версия wesh, вместо `https://nekit270.ch/getwesh` используйте `https://nekit270.ch/getwesh86`.

## Документация
Документация wesh доступна на [GitHub Pages](https://nekit270ch.github.io/wesh).

## Сборка
### Сборка с помощью Visual Studio
Откройте в Visual Studio 2022 файл `src\wesh.sln`.
### Сборка из командной строки
С помощью компилятора CSC wesh можно собрать следующими командами (предполагается, что она будет запущена в папке `src`):
64-bit: `csc /nologo /r:Microsoft.JScript.dll,System.IO.Compression.FileSystem.dll /platform:x64 /out:wesh.exe *.cs`
32-bit: `csc /nologo /r:Microsoft.JScript.dll,System.IO.Compression.FileSystem.dll /platform:x86 /out:wesh_x86.exe *.cs`
