import os
import subprocess
import json
import glob
import sys
# JSONファイルを開いて読み込む
with open("prompt.json", "r", encoding="utf-8") as f:
    data = json.load(f)
# --- 設定 ---
input_folder = data["pngs_path"]     # PNGフォルダ
output_folder = data["dds_path"]    # DDS保存先
texconv_path = data["texconv_path"]    # texconv のパス（PATHが通っていなければフルパスで）

# --- 出力フォルダを作成 ---
os.makedirs(output_folder, exist_ok=True)

non_png_found = False
if os.path.isdir(output_folder):
    # 再帰的に走査
    for root, dirs, files in os.walk(output_folder):
        for file in files:
            if not file.lower().endswith(".dds"):
                print(f"dds以外のファイルを発見: {os.path.join(root, file)}")
                non_png_found = True

    if non_png_found:
        print("dds以外のファイルが存在します。問題ないか確認して手動削除してください")
        sys.exit()
    else:
        # 再帰的にすべてのPNGファイルを取得
        dds_files = glob.glob(os.path.join(output_folder, "**", "*.dds"), recursive=True)

        # ファイル削除処理
        for file in dds_files:
            try:
                os.remove(file)
                print(f"削除: {file}")
            except Exception as e:
                print(f"エラー: {file} - {e}")

        print("完了！")


# --- PNGファイルを走査して変換 ---
for filename in os.listdir(input_folder):
    if filename.lower().endswith(".png"):
        input_path = os.path.join(input_folder, filename)

        # texconvコマンド（BC1 / DXT1, ミップなし, 線形色空間）
        command = [
            texconv_path,
            "-f", "BC1_UNORM",          # フォーマット指定（DXT1相当）
            "-nologo",                  # ロゴを非表示
            "-o", output_folder,        # 出力フォルダ
            "-y",                       # 上書き確認しない
            "-pmalpha",                 # プレマルチアルファ除去（必要に応じて外してOK）
            "-nogpu",                   # GPU非使用（安定化）
            "-m", "1",                  # ミップマップなし（必要なら -m 0 にして自動）
            input_path
        ]

        print("実行:", " ".join(command))

        result = subprocess.run(command, capture_output=True, text=True)
        if result.returncode != 0:
            print(f"[失敗] {filename}\n{result.stderr}")
        else:
            print(f"[成功] {filename} → .dds")