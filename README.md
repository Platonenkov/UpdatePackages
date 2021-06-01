## UpdatePackages
Использовать в больших проектах для ускоренного обновления пакетов минуя студию.

Quick start
Запустить файл
```
UpdatePackages.exe
```
Программа в первом запуске создаст файл настроек `UpdatePackagesScheme.json`
Внесите изменения в файл, а именно укажите какие библиотеки изменять -используйте секцию `Packages`
```Json
"Packages": [
		{
			"Library": "RRJ-Express.ContainerCore",
			"OldVersion": "1.1.1.5",
			"NewVersion": "1.1.1.6"
		}
	]
```
в разделе `Sections` указаны типы файлов или их название которые будет искать программа (FileMask), в секции `Regular` указаны выражения поиска
Например если выражение `"{Name}, Version={version}"` то программа будет искать в файлах запись вида  "RRJ-Express.ContainerCore, Version=1.1.1.5" и менять версию на новую

{Name} - указывает что поиск ведётся по имени как вписано в секции Library
{name} - указывает что поиск ведётся по ToLower ()
```
"{name}\\{version}"
"rrj-express.containercore\1.1.1.5"
```

### Базовые настройки секций поиска
```Json
"Sections": [
		{
			"FileMask": "*.csproj",
			"Regular": [
				"{Name}, Version={version}",
				"{Name}\" Version=\"{version}",
				"{name}\\{version}",
				"{Name}.{version}"
			]
		},
		{
			"FileMask": "packages.config",
			"Regular": [
				"{Name}\" Version=\"{version}"
			]
		}
	]
```
