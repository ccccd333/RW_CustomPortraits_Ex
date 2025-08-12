@echo off
setlocal

REM 保存時のカレントディレクトリを記録
set START_DIR=%CD%

REM チェック：mp42gif フォルダがあるか
if not exist "%START_DIR%\mp42gif" (
    echo [エラー] ./mp42gif フォルダが見つかりません。
    pause
    exit /b 1
)

REM チェック：input.mp4 があるか
if not exist "%START_DIR%\mp42gif\input.mp4" (
    echo [エラー] ./mp42gif/input.mp4 が見つかりません。
    pause
    exit /b 1
)

REM 処理の開始
echo === mp42gif/input.mp4 の検出OK ===

REM カレントディレクトリを元に戻す
cd /d "%START_DIR%"

REM ステップ 1
echo === Step 1: MP4 → GIF ===
python convert_mp42gif_quo.py
if errorlevel 1 (
    echo [エラー] convert_mp42gif_quo.py 実行失敗。
    pause
    exit /b 1
)

REM ステップ 2
echo === Step 2: GIF → PNG ===
python convert_gif2png.py
if errorlevel 1 (
    echo [エラー] convert_gif2png.py 実行失敗。
    pause
    exit /b 1
)

REM ステップ 3
echo === Step 3: PNG → DDS ===
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
