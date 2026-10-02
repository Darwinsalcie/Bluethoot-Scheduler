# BluetoothScheduler

Utilidad nativa para Windows en **.NET 10** / C# diseñada para encender y apagar automáticamente el radio Bluetooth del equipo según un horario y días configurables, con una interfaz minimalista estilo Google/Material y bajo consumo de recursos.

---

## 🏛️ Arquitectura del Proyecto

La solución consta de 3 proyectos principales y un proyecto de pruebas:

1. **`BluetoothScheduler.Core`** (`net10.0`): Lógica pura y reglas de negocio, sin dependencias del sistema operativo Windows ni de la interfaz gráfica.
   - [ScheduleConfig](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Core/Models/ScheduleConfig.cs): Configuración de días activos, horas On/Off, ForceOff, inicio con Windows y notificaciones.
   - [ScheduleState](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Core/Models/ScheduleState.cs): Estado persistido (últimas fechas de ejecución y pausa).
   - [SchedulePolicy](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Core/Logic/SchedulePolicy.cs): Función pura evaluadora determinista y testeable.
   - [Interfaces](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Core/Interfaces/Interfaces.cs): `IClock`, `IBluetoothController`, `IConfigStore`, `IStateStore`, `IStartupRegistrar`, `ILogger`.

2. **`BluetoothScheduler.Infrastructure`** (`net10.0-windows10.0.19041.0`): Implementaciones de bajo nivel para Windows.
   - [WinRtBluetoothController](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Infrastructure/Bluetooth/WinRtBluetoothController.cs): Control nativo con `Windows.Devices.Radios` (sin NuGets externos ni librerías pesadas).
   - [JsonConfigStore](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Infrastructure/Persistence/JsonConfigStore.cs) & [JsonStateStore](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Infrastructure/Persistence/JsonStateStore.cs): Guardado atómico en `%AppData%\BluetoothScheduler\`.
   - [StartupRegistrar](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Infrastructure/Services/StartupRegistrar.cs): Clave `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
   - [FileLogger](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Infrastructure/Services/FileLogger.cs): Registro rotativo simple a archivo `app.log`.
   - [SystemClock](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.Infrastructure/Services/SystemClock.cs): Reloj del sistema.

3. **`BluetoothScheduler.App`** (`net10.0-windows10.0.19041.0` - WinForms):
   - [Program](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/Program.cs): Mutex de instancia única, `PerMonitorV2` HighDPI e inyección de dependencias manual.
   - [AppScheduler](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/Services/AppScheduler.cs): Orquestador reactivo (Timer al próximo límite de ventana, `PowerModeChanged`, `SessionSwitch`, `TimeChanged`, `Radio.StateChanged`).
   - [TrayApplicationContext](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/TrayApplicationContext.cs): Menú e icono en bandeja del sistema.
   - [Theme](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/UI/Theme.cs): Tokens de diseño Google/Material, compatibilidad con tema claro/oscuro y recursos GDI+ cacheados.
   - [SettingsForm](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/UI/SettingsForm.cs) y Controles personalizados ([DayChip](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/UI/Controls/DayChip.cs), [ToggleSwitch](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/UI/Controls/ToggleSwitch.cs), [TimeField](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/UI/Controls/TimeField.cs), [FlatButton](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/UI/Controls/FlatButton.cs), [SettingRow](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/src/BluetoothScheduler.App/UI/Controls/SettingRow.cs)).

4. **`BluetoothScheduler.Tests`** (`net10.0` - xUnit):
   - [SchedulePolicyTests](file:///c:/Proyectos%20de%20Software/Auto%20Bluethoot/tests/BluetoothScheduler.Tests/SchedulePolicyTests.cs): 10 escenarios completos con reloj simulado.

---

## 🚀 Compilación y Ejecución

### 1. Requisitos
- .NET 10 SDK (`10.0.x`) o superior.
- Windows 10 (Build 19041+) o Windows 11.

### 2. Ejecutar pruebas unitarias
```powershell
dotnet test
```

### 3. Compilar la solución
```powershell
dotnet build
```

### 4. Ejecutar la aplicación en desarrollo
```powershell
dotnet run --project src/BluetoothScheduler.App
```

### 5. Publicación para distribución
#### Publicación estándar (Framework-dependent):
```powershell
dotnet publish src/BluetoothScheduler.App -c Release -o ./publish
```

#### Publicación Single-File ejecutable único autónomo (Self-Contained x64):
```powershell
dotnet publish src/BluetoothScheduler.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish-singlefile
```
