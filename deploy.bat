

set H=R:\KSP_1.3.1_dev
echo %H%

copy /Y PreciseNode\bin\Debug\PreciseNode.dll GameData\PreciseNode\plugins
copy /Y PreciseNode\etc\PreciseNode.version GameData\PreciseNode\plugins

cd GameData
mkdir "%H%\GameData\PreciseNode"
xcopy /y /s PreciseNode "%H%\GameData\PreciseNode"

