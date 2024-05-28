# Путь к проекту
$project_path = "..\Chain\Chain.csproj"

# Выполнение сборки проекта
Write-Host "Building the project..."
dotnet build $project_path
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Exiting script."
    exit $LASTEXITCODE
}

# Массив значений для X
$values = @(4, 17, -5, 32, 4, 5)

# Количество процессов
$num_processes = 6

# Базовый порт для прослушивания
$base_port = 1234

# Хост для следующего соседа (для простоты используем localhost)
$next_host = "localhost"

# Путь к скомпилированному исполняемому файлу
$executable_path = "..\Chain\bin\Debug\net8.0\Chain.exe"

# Запуск процессов
for ($i = 0; $i -lt $num_processes; $i++) {
    $listening_port = $base_port + $i
    $next_port = $base_port + (($i + 1) % $num_processes)
    $is_initiator = "true"

    # Установить параметр инициатора для первого процесса
    if ($i -gt 0) {
        $is_initiator = "false"
    }

    # Запуск процесса в новом окне PowerShell
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "`$value = $($values[$i]); echo `$value; `$value | & '$executable_path' $listening_port $next_host $next_port $is_initiator"
}
