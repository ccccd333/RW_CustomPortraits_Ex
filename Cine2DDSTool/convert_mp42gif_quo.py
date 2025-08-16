import subprocess
import json

# JSONファイルを開いて読み込む
with open("prompt.json", "r", encoding="utf-8") as f:
    data = json.load(f)

ffmpeg = data["ffmpeg"]
magick = data["magick"]
#input_video = "input.mp4"
palette = "palette.png"
#output_gif = "output.gif"
optimized_gif = "optimized.gif"

input_video = data["video_path"]
output_gif = data["gif_path"]
fps = data["fps"]

# 1. パレット作成
vf = "fps=" + str(fps) + ",scale=iw:ih:flags=lanczos,palettegen"

subprocess.run([
    ffmpeg, "-i", input_video,
    "-vf", vf,
    "-y", palette
], check=True)

# 2. パレットを使ってGIF生成
filter_complex = "fps=" + str(fps) + ",scale=iw:ih:flags=lanczos[x];[x][1:v]paletteuse=dither=bayer:bayer_scale=5"

subprocess.run([
    ffmpeg, "-i", input_video, "-i", palette,
    "-filter_complex", filter_complex,
    "-y", output_gif
], check=True)

# 3. ImageMagickで最適化
subprocess.run([
    magick, output_gif,
    "-define", "dds:compression=dxt1",
    "-coalesce", "-layers", "Optimize", "-colors", "256",
    optimized_gif
], check=True)

print(f"最高品質GIF作成完了: {optimized_gif}")