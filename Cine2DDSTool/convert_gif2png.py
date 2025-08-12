import subprocess
import sys
import os
import glob
# Pillow を自動インストール
try:
    from PIL import Image
except ImportError:
    print("Pillow が見つかりません。インストールします...")
    subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow"])
    from PIL import Image

import json

# JSONファイルを開いて読み込む
with open("prompt.json", "r", encoding="utf-8") as f:
    data = json.load(f)

# --- GIF を PNG に変換 ---
gif_path = data["gif_path"]
output_dir = data["pngs_path"]

cutin_name = data["cutin_name"]
resize_width = data["resize_width"]
resize_height = data["resize_height"]
resize_size = (resize_width, resize_height)

os.makedirs(output_dir, exist_ok=True)

gif = Image.open(gif_path)
frame = 1

# non_png_found = False
# if os.path.isdir(output_dir):
#     # 再帰的に走査
#     for root, dirs, files in os.walk(output_dir):
#         for file in files:
#             if not file.lower().endswith(".png"):
#                 print(f"PNG以外のファイルを発見: {os.path.join(root, file)}")
#                 non_png_found = True

#     if non_png_found:
#         print("PNG以外のファイルが存在します。問題ないか確認して手動削除してください")
#     else:
#         # 再帰的にすべてのPNGファイルを取得
#         png_files = glob.glob(os.path.join(output_dir, "**", "*.png"), recursive=True)

#         # ファイル削除処理
#         for file in png_files:
#             try:
#                 os.remove(file)
#                 print(f"削除: {file}")
#             except Exception as e:
#                 print(f"エラー: {file} - {e}")

#         print("完了！")

while True:
    gif.seek(frame)

    im_rgba = gif.convert("RGBA").resize(resize_size, Image.LANCZOS)
    frame_path = os.path.join(output_dir, cutin_name+f"{frame:d}.png")
    im_rgba.save(frame_path)
    print(f"保存: {frame_path}")
    frame += 1
    try:
        gif.seek(frame)
    except EOFError:
        break