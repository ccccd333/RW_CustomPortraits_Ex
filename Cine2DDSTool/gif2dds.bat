@echo off
setlocal

REM 保存時のカレントディレクトリを記録
set START_DIR=%CD%

REM チェック：gif2png フォルダがあるか
if not exist "%START_DIR%\gif2png" (
    echo [エラー] ./gif2png フォルダが見つかりません。
    pause
    exit /b 1
)

REM チェック：output.gif があるか
if not exist "%START_DIR%\gif2png\output.gif" (
    echo [エラー] ./gif2png/output.gif が見つかりません。
    pause
    exit /b 1
)

REM ステップ 1
echo === Step 1: GIF → PNG ===
python convert_gif2png.py
if errorlevel 1 (
    echo [エラー] convert_gif2png.py 実行失敗。
    pause
    exit /b 1
)

REM ステップ 2
echo === Step 2: PNG → DDS ===
python convert_png2dds.py
if errorlevel 1 (
    echo [エラー] convert_png2dds.py 実行失敗。
    pause
    exit /b 1
)

echo.
echo すべての処理が完了しました。png2ddsをご確認ください。
pause
endlocal
