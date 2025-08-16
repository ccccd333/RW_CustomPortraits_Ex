import os
import subprocess
import json

# JSONファイルを開いて読み込む
with open("prompt.json", "r", encoding="utf-8") as f:
    data = json.load(f)

# 設定
input_video = data["video_path"]
output_gif = data["gif_path"]

texconv_path = data["texconv_path"]   # texconv.exe のパス
input_root = data["vflip_input_path"]   # 入力フォルダ
output_root = data["vflip_output_path"] # 出力フォルダ

# 再帰的にDDSを処理
for dirpath, dirnames, filenames in os.walk(input_root):
    for filename in filenames:
        if filename.lower().endswith(".dds"):
            input_file = os.path.join(dirpath, filename)
            
            # 入力フォルダからの相対パスを作成
            rel_path = os.path.relpath(dirpath, input_root)
            output_dir = os.path.join(output_root, rel_path)
            os.makedirs(output_dir, exist_ok=True)
            
            # texconv コマンド作成
            command = [
                texconv_path,
                "-vflip",
                "-y",
                "-o", output_dir,
                input_file
            ]
            
            print(f"Processing: {input_file}")
            subprocess.run(command)
