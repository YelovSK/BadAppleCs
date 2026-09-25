Ray marched Bad Apple using raylib.

<img width="1813" height="1359" alt="image" src="https://github.com/user-attachments/assets/60a30976-dac0-43aa-90f9-1a8bafb06217" />

https://github.com/user-attachments/assets/cfd243db-055e-4afe-a923-d201ea2defe3

## Get video frames and audio

Any resolution works. These steps use the 1440p (1920×1440, 30 fps, ~350 MB) upscale from the [Internet Archive](https://archive.org/details/bad-apple-resources). Requires [ffmpeg](https://ffmpeg.org/).

Run from the `Application` folder:

1. `curl -L -o bad_apple.mp4 "https://archive.org/download/bad-apple-resources/bad_apple%401440p.mp4"`

2. `ffmpeg -i bad_apple.mp4 -pix_fmt gray resources/image_sequence/%d.png`

3. `ffmpeg -i bad_apple.mp4 -vn resources/bad_apple.wav`

The audio is optional.
