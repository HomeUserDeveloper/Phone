@echo off
set "APP=%~dp0bin\Debug\net10.0-windows\Phonebook.exe"

if not exist "%APP%" (
    echo Сборка не найдена. Выполняю dotnet build...
    cd /d "%~dp0"
    dotnet build
    if errorlevel 1 (
        echo Ошибка сборки.
        pause
        exit /b 1
    )
)

if exist "%APP%" (
    echo Запуск приложения...
    echo Нажмите любую клавишу, чтобы закрыть окно запуска.
    start "" "%APP%"
    pause
    exit /b 0
) else (
    echo Не удалось найти приложение после сборки.
    pause
    exit /b 1
)
