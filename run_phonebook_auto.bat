@echo off
setlocal
set "APP=d:\proj\test\bin\Debug\net10.0-windows\Phonebook.exe"

if exist "%APP%" (
    echo Запуск Phonebook...
    start "" "%APP%"
    exit /b 0
)

echo EXE не найден. Собираю проект...
cd /d "d:\proj\test"
dotnet build

if exist "%APP%" (
    echo Запуск Phonebook...
    start "" "%APP%"
    exit /b 0
) else (
    echo Не удалось собрать приложение.
    pause
    exit /b 1
)
