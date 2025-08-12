@echo off
setlocal

REM �ۑ����̃J�����g�f�B���N�g�����L�^
set START_DIR=%CD%

REM �`�F�b�N�Fgif2png �t�H���_�����邩
if not exist "%START_DIR%\gif2png" (
    echo [�G���[] ./gif2png �t�H���_��������܂���B
    pause
    exit /b 1
)

REM �`�F�b�N�Foutput.gif �����邩
if not exist "%START_DIR%\gif2png\output.gif" (
    echo [�G���[] ./gif2png/output.gif ��������܂���B
    pause
    exit /b 1
)

REM �X�e�b�v 1
echo === Step 1: GIF �� PNG ===
python convert_gif2png.py
if errorlevel 1 (
    echo [�G���[] convert_gif2png.py ���s���s�B
    pause
    exit /b 1
)

REM �X�e�b�v 2
echo === Step 2: PNG �� DDS ===
python convert_png2dds.py
if errorlevel 1 (
    echo [�G���[] convert_png2dds.py ���s���s�B
    pause
    exit /b 1
)

echo.
echo ���ׂĂ̏������������܂����Bpng2dds�����m�F���������B
pause
endlocal
