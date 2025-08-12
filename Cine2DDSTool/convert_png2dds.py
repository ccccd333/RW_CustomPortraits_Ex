import os
import subprocess
import json

# JSONファイルを開いて読み込む
with open("prompt.json", "r", encoding="utf-8") as f:
    data = json.load(f)
# --- 設定 ---
input_folder = data["pngs_path"]     # PNGフォルダ
output_folder = data["dds_path"]    # DDS保存先
texconv_path = data["texconv_path"]    # texconv のパス（PATHが通っていなければフルパスで）

# --- 出力フォルダを作成 ---
os.makedirs(output_folder, exist_ok=True)

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