import subprocess
import sys
import json

# JSONファイルを開いて読み込む
with open("prompt.json", "r", encoding="utf-8") as f:
    data = json.load(f)

# moviepy がインストールされていなければインストール
try:
    from moviepy import VideoFileClip
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "moviepy"])
    from moviepy import VideoFileClip

# --- 設定 ---
input_video = data["video_path"]
output_gif = data["gif_path"]

start_time = data["n_start_time"]       # 秒
#end_time = 4.61         # 秒（つまり5秒分）

target_frames = data["target_frames"]
#resize_width = data["n_resize_width"]
#resize_height = data["n_resize_height"]

# --- 動画読み込みと切り出し ---
clip = VideoFileClip(input_video).subclipped(start_time)

# --- サイズ変更（リサイズ） ---
#clip = clip.resized(new_size=(resize_width, resize_height))

# --- GIFに書き出し ---
clip.write_gif(output_gif, fps = target_frames)


print(f"GIF作成完了: {output_gif}")

