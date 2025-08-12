必須：
最新のpython（3.12 以降）
https://www.python.org/downloads/release/python-3135/

textconv.exe
https://github.com/microsoft/DirectXTex/releases/tag/mar2025

推奨：(ビデオ→gif変換で劣化がひどいので)

ImageMagick-7.1.1-47-Q16-x64-dll.exe
https://www.imagemagick.org/script/download.php#windows

ffmpeg-release-essentials.zip
https://www.gyan.dev/ffmpeg/builds/

用意①：
まずprompt.jsonを開きます。
  "以下はtexconv用パス":"",
  "texconv_path": "C:/convert/texconv/texconv.exe",→必須のtexconv.exeのパス
  
  "以下はクオリティ用videoパス":"",
  "ffmpeg" : "C:/convert/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe",→推奨のffmpegのパス
  "magick" : "C:/Program Files/ImageMagick-7.1.1-Q16/magick.exe",→推奨のimagemagickのパス。インストーラーでCかDに入ってるはず。

上記を記述し終えたら以下です。

  "video_path": "C:/convert/mp42gif/input.mp4",→mp4をgifに変換したいものをここに配置
  "gif_path": "C:/convert/gif2png/output.gif",→mp4をgifに変換したもののディレクトリ
  "pngs_path": "C:/convert/gif2png/output_pngs",→gifをpng(フレーム毎)に変換したもののディレクトリ
  "dds_path": "C:/convert/png2dds",→pngをddsに変換したもののディレクトリ
  
  "以下はカットイン名":"",
  "cutin_name" : "cutinidlepm",→gifからpngへ変換時のカットイン名

gifからpngの際のサイズ変更用です
  "resize_width" : 100,→pngの横幅
  "resize_height" : 100,→pngの縦幅

mp4からgifの際の設定は以下です
以下はmp4→gifクオリティ用です
  "以下はクオリティ用Video2gifの設定":"",
  "fps" : 12,→本来のビデオのフレームレートで設定してください。ただ、png枚数が多い場合は本来のフレームレートより落として設定してください。mp4→プロパティ→詳細→フレーム率

以下はmp4→gifです(pythonライブラリ)
  "以下はVideo2gifの設定":"",
  "n_start_time" : 0,→gifに変換時にmp4の開始秒数を指定できます。
  "target_frames" : 12,→このフレームでgifを作成します。

  
用意②
上記必須と推奨をダウンロードしたら

１．
ffmpeg-7.1.1-essentials_build.zipは
convert配下に解凍してください。

２．
ImageMagick-7.1.1-47-Q16-x64-dll.exe
こちらはインストーラーの指示にしたがってインストールすれば大丈夫です。

３．
textconv.exe
こちらはconvert/texconv配下に置いてください。

使い方と注意点
パターン①mp4からアニメーション用dds作成
１．(推奨をインストールしている)→convert_mp42gif_quo.py
　　(推奨をインストールしていない)→convert_mp42gif.py
　　　　　　　　　　　　　　　　　　こちらはpythonのpipを使っているのでウイルスチェックに引っかかるかもですが、無害です。
　　　　　　　　　　　　　　　　　　単にmoviepyライブラリを取ってきているだけです。
　　　　　　　　　　　　　　　　　　そもそもpipがウイルス的な動きをするという面倒なことしているのでキャプチャされます。
２．convert\gif2png\output_pngsをディレクトリ自体を消す

３．convert_gif2png.py実行
　　ここもpillowをpipしているのでウイルスチェックで引っかかる場合は一時的に通してあげる必要があります。

４．convert_png2dds.py実行

パターン②gifからアニメーション用dds作成
１．convert_gif2png.py実行
　　ここもpillowをpipしているのでウイルスチェックで引っかかる場合は一時的に通してあげる必要があります。

２．convert_png2dds.py実行

完了したら
convert\png2dds配下にddsになっているのでこれをつかってください。