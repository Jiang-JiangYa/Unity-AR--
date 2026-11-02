"""Annotate and merge the two supplied demo recordings; originals are read-only.

Dependency: Pillow and imageio-ffmpeg (the latter may be in .tools/video-deps).
Run from any directory with Python 3.10+; generated intermediates stay in .tools.
"""
import argparse
import json
import math
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.tools/video-deps'))
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

PHONE = Path(r'E:\yangjia\Downloads\video_20261102_234808_edit.mp4')
BACKEND = Path(r'E:\yangjia\Videos\屏幕录制\屏幕录制 2026-11-02 234321.mp4')
WORK = ROOT / '.tools/video-work/render'
OUT = ROOT
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
W, H, FPS = 1920, 1080, 30
BG = '#101b29'
PANEL = '#17283b'
TEAL = '#3dd9bd'
WHITE = '#f4f8fc'
MUTED = '#a9bdce'


def font(size, bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc' if bold else 'C:/Windows/Fonts/msyh.ttc', size)


def wrapped(draw, text, xy, width, size=30, fill=WHITE, bold=False, spacing=14):
    f = font(size, bold)
    y = xy[1]
    for paragraph in text.split('\n'):
        line = ''
        for char in paragraph:
            if line and draw.textlength(line + char, font=f) > width:
                draw.text((xy[0], y), line, font=f, fill=fill)
                y += size + spacing
                line = ''
            line += char
        draw.text((xy[0], y), line, font=f, fill=fill)
        y += size + spacing
    return y


def base(chapter):
    im = Image.new('RGB', (W, H), BG)
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, W, 7), fill=TEAL)
    d.text((58, 32), 'PARK INSPECTION  /  功能实录', font=font(25, True), fill=WHITE)
    label = chapter
    d.text((W - 60 - d.textlength(label, font=font(25)), 33), label, font=font(25), fill=MUTED)
    return im, d


def phone_bg(stage):
    im, d = base('01  /  Unity AR 与 React 管理端')
    d.rounded_rectangle((82, 92, 512, 1028), radius=22, fill='#263c52')
    d.text((94, 1040), '手机端完整画面', font=font(21), fill=MUTED)
    d.text((600, 116), stage['title'], font=font(46, True), fill=WHITE)
    wrapped(d, stage['description'], (603, 188), 1200, 28, MUTED, spacing=10)
    d.rounded_rectangle((590, 287, 1830, 925), radius=18, fill=PANEL)
    d.text((612, 298), stage['detail_label'], font=font(22), fill=TEAL)
    wrapped(d, stage['caption'], (603, 945), 1210, 31, WHITE, spacing=10)
    if stage.get('speed', 1) != 1:
        badge = f"片段加速 ×{stage['speed']:g}" if stage['speed'] > 1 else '慢放查看'
        d.text((1530, 1037), badge, font=font(22), fill=MUTED)
    if stage.get('freeze'):
        d.text((1560, 1037), '成功回执 · 定格', font=font(22), fill=MUTED)
    return im


def backend_bg(stage):
    im, d = base('02  /  Go 服务与 JSON 持久化')
    d.text((65, 102), stage['title'], font=font(39, True), fill=WHITE)
    # Source chrome and taskbar are cropped; the operation area remains visible.
    d.rounded_rectangle((58, 165, 1862, 936), radius=16, fill=PANEL)
    wrapped(d, stage['caption'], (74, 955), 1770, 34, WHITE, spacing=10)
    return im


def card(kind):
    im, d = base('Unity LTS  /  C#  /  Go  /  React + TypeScript')
    if kind == 'intro':
        d.text((130, 215), '园区 AR 巡检', font=font(90, True), fill=WHITE)
        d.text((137, 349), '从手机现场上报，到网页处理与重启验证', font=font(42), fill=MUTED)
        labels = ['AR 放置标记', 'Go 保存问题', 'React 更新状态', '重启保留数据']
        for i, label in enumerate(labels):
            x = 130 + i * 440
            d.rounded_rectangle((x, 535, x + 405, 710), radius=20, fill=PANEL)
            d.text((x + 26, 560), f'0{i + 1}', font=font(34, True), fill=TEAL)
            d.text((x + 26, 626), label, font=font(30, True), fill=WHITE)
        d.text((137, 866), '两段真实录屏合辑  ·  原声保留  ·  关键操作配说明', font=font(30), fill=MUTED)
    else:
        d.text((130, 215), '演示流程完成', font=font(78, True), fill=WHITE)
        items = ['现场扫描平面、放置标记并上报', '网页查看问题并修改处理状态', '停止服务时提示错误，重启后原记录仍保留']
        for i, item in enumerate(items):
            y = 395 + i * 145
            d.ellipse((140, y + 10, 178, y + 48), fill=TEAL)
            d.text((215, y), item, font=font(40), fill=WHITE)
        d.text((137, 925), 'Unity AR  →  Go API  →  JSON 文件  →  React 管理页', font=font(31), fill=MUTED)
    return im


STAGES = [
    dict(kind='card', name='intro', duration=3.2, caption='园区 AR 巡检：手机上报、网页处理、重启保留数据'),
    dict(kind='phone', start=0, end=23.5, speed=1.6, title='01  扫描真实水平面',
         description='缓慢移动手机，让 AR Foundation 检测环境中的水平面。',
         detail_label='现场画面 · 局部放大', crop=(720, 540, 0, 400),
         caption='绿色区域是已检测到的平面；顶部显示 SessionTracking 与平面数量。'),
    dict(kind='phone', start=23.5, end=27, title='02  点击平面，标记问题位置',
         description='球形标记代表本次选中的巡检位置。',
         detail_label='放置标记 · 局部放大', crop=(720, 540, 0, 400),
         caption='点击检测到的平面，生成位置标记，并记录 x / y / z 坐标。'),
    dict(kind='phone', start=27, end=37.8, speed=1.3, title='03  填写问题并选择优先级',
         description='标题必填，描述可选；本次选择 low（低优先级）。',
         detail_label='问题表单 · 局部放大', crop=(720, 560, 0, 1000),
         caption='填写标题、切换优先级，然后点击 Submit issue 提交。'),
    dict(kind='phone', start=37.8, end=39, title='04  上报问题，等待后端回执',
         description='Unity 将问题内容与 AR 坐标发送到同一个 Go 后端。',
         detail_label='提交回执 · 局部放大', crop=(720, 420, 0, 450),
         caption='提交成功后弹窗反馈，并返回后端生成的问题 ID。'),
    dict(kind='phone', freeze=39, duration=2.5, title='提交成功：问题已保存',
         description='定格展示原视频中的成功弹窗与唯一问题 ID。',
         detail_label='真实成功回执 · 定格放大', crop=(720, 420, 0, 450),
         caption='Submitted successfully：后端已返回保存成功的结果。'),
    dict(kind='phone', start=39, end=41.5, title='回到现场，准备查看管理页',
         description='关闭成功弹窗后，可继续上报下一条问题。',
         detail_label='手机原画面 · 局部放大', crop=(720, 560, 0, 1000),
         caption='当前标记与已提交的标题清除；问题记录保存在后端。'),
    dict(kind='phone', start=41.5, end=57, speed=1.5, title='05  在 React 管理页刷新列表',
         description='管理端从同一个 Go API 获取手机上报的问题。',
         detail_label='电脑管理页 · 原视频局部放大', crop=(720, 560, 0, 240),
         caption='点击“刷新列表”，查看刚提交的问题、优先级与位置坐标。'),
    dict(kind='phone', start=57, end=69, title='06  更新问题处理状态',
         description='在管理页选择状态，保存后更新列表与统计。',
         detail_label='React 状态操作 · 原视频局部放大', crop=(720, 560, 0, 240),
         caption='本次将问题改为“已解决”，处理状态由 Go 后端保存。'),
    dict(kind='phone', start=69, end=74.5, speed=1.5, title='手机与网页使用同一份数据',
         description='下面继续展示 Go 服务停止与重启后的数据表现。',
         detail_label='管理页结果 · 原视频局部放大', crop=(720, 560, 0, 240),
         caption='第一部分：AR 放置 → 手机提交 → 网页查看 → 修改状态。'),
    dict(kind='backend', start=0, end=8.5, title='07  查看重启前的问题与状态',
         caption='管理页中已有问题记录；本次演示统计为待处理 5、处理中 0、已解决 2。', crop=(2550, 1290, 0, 120)),
    dict(kind='backend', start=8.5, end=12.7, title='08  在终端停止 Go 服务',
         caption='在运行后端的终端按 Ctrl+C，停止当前 Go 服务。', crop=(1800, 910, 200, 220)),
    dict(kind='backend', start=12.7, end=18.8, title='09  后端停止，网页明确提示连接失败',
         caption='点击“刷新列表”，页面出现错误提示和“重试”，没有直接白屏。', crop=(2550, 1290, 0, 120)),
    dict(kind='backend', start=18.8, end=22.2, title='10  在相同目录重新启动后端',
         caption='再次执行 go run .；后端继续使用 backend/data/issues.json。', crop=(1800, 910, 200, 220)),
    dict(kind='backend', start=22.2, end=27.9, title='11  刷新后，原有记录和处理状态仍在',
         caption='服务恢复后重新读取列表，原有问题与“已解决”状态保留。', crop=(2550, 1290, 0, 120)),
    dict(kind='card', name='outro', duration=3.2, caption='现场上报、网页更新、关闭与重启后读取：演示流程完成'),
]


def run(cmd, log):
    with log.open('w', encoding='utf-8') as stream:
        process = subprocess.run([FFMPEG, '-hide_banner', '-loglevel', 'warning', '-y', *cmd], stdout=stream, stderr=stream)
    if process.returncode:
        raise RuntimeError(log.read_text(encoding='utf-8', errors='replace')[-5000:])


def render(index, stage):
    name = f'{index:02d}'
    output = WORK / f'{name}.mp4'
    if 'duration' not in stage:
        stage['duration'] = (stage['end'] - stage['start']) / stage.get('speed', 1)
    # Integer frame lengths make concat time boundaries predictable.
    duration = round(stage['duration'] * FPS) / FPS
    stage['duration'] = duration
    bg = WORK / f'{name}.png'
    image = card(stage['name']) if stage['kind'] == 'card' else phone_bg(stage) if stage['kind'] == 'phone' else backend_bg(stage)
    image.save(bg)
    encoding = ['-t', str(duration), '-r', str(FPS), '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '20', '-pix_fmt', 'yuv420p',
                '-c:a', 'aac', '-b:a', '160k', '-ar', '48000', '-ac', '2', '-map_metadata', '-1', '-movflags', '+faststart', str(output)]
    if stage['kind'] == 'card':
        run(['-loop', '1', '-framerate', str(FPS), '-i', str(bg), '-f', 'lavfi', '-i', 'anullsrc=r=48000:cl=stereo',
             '-map', '0:v', '-map', '1:a', *encoding], WORK / f'{name}.log')
        return output
    src = PHONE if stage['kind'] == 'phone' else BACKEND
    speed = stage.get('speed', 1)
    if 'freeze' in stage:
        still = WORK / f'{name}-still.png'
        run(['-ss', str(stage['freeze']), '-i', str(src), '-frames:v', '1', str(still)], WORK / f'{name}-still.log')
        inputs = ['-loop', '1', '-framerate', str(FPS), '-i', str(still), '-loop', '1', '-framerate', str(FPS), '-i', str(bg),
                  '-f', 'lavfi', '-i', 'anullsrc=r=48000:cl=stereo']
        audio = '[2:a]anull[a]'
    else:
        inputs = ['-ss', str(stage['start']), '-t', str(stage['end'] - stage['start']), '-i', str(src),
                  '-loop', '1', '-framerate', str(FPS), '-i', str(bg)]
        audio = f'[0:a]asetpts=PTS-STARTPTS,atempo={speed},aresample=48000,aformat=channel_layouts=stereo,afade=t=in:d=0.05,afade=t=out:st={max(0,duration-.06)}:d=0.06[a]'
    cw, ch, cx, cy = stage['crop']
    if stage['kind'] == 'phone':
        dw = int(cw * min(1190 / cw, 575 / ch)) // 2 * 2
        dh = int(ch * min(1190 / cw, 575 / ch)) // 2 * 2
        dx, dy = 610 + (1200 - dw) // 2, 342 + (575 - dh) // 2
        vf = f'[0:v]setpts=(PTS-STARTPTS)/{speed},fps={FPS},setsar=1,split=2[p][d];[p]scale=414:920[pv];[d]crop={cw}:{ch}:{cx}:{cy},scale={dw}:{dh}[dv];[1:v][pv]overlay=90:100[base];[base][dv]overlay={dx}:{dy}[out]'
    else:
        vf = f'[0:v]setpts=PTS-STARTPTS,fps={FPS},setsar=1,crop={cw}:{ch}:{cx}:{cy},scale=1760:746:force_original_aspect_ratio=decrease,pad=1760:746:(ow-iw)/2:(oh-ih)/2:color=0x17283b[v];[1:v][v]overlay=80:177[out]'
    run([*inputs, '-filter_complex', vf + ';' + audio, '-map', '[out]', '-map', '[a]', *encoding], WORK / f'{name}.log')
    return output


def timestamp(seconds):
    ms = round(seconds * 1000)
    return f'{ms//3600000:02}:{ms//60000%60:02}:{ms//1000%60:02},{ms%1000:03}'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview', action='store_true', help='Render only the frozen success receipt preview.')
    args = parser.parse_args()
    WORK.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    if args.preview:
        print(render(5, STAGES[5]), flush=True)
        return
    files = []
    cursor = 0
    subtitles = []
    timeline = []
    for i, stage in enumerate(STAGES):
        print(f'Rendering {i+1}/{len(STAGES)}: {stage.get("title",stage.get("name"))}', flush=True)
        files.append(render(i, stage))
        end = cursor + stage['duration']
        subtitles.append(f'{i+1}\n{timestamp(cursor)} --> {timestamp(end)}\n{stage["caption"]}\n')
        timeline.append(dict(stage, output_start=round(cursor,3), output_end=round(end,3)))
        cursor = end
    concat = WORK / 'concat.txt'
    concat.write_text(''.join(f"file '{p.as_posix()}'\n" for p in files), encoding='utf-8')
    target = OUT / '园区AR巡检_演示合辑.mp4'
    run(['-f', 'concat', '-safe', '0', '-i', str(concat), '-c:v', 'copy', '-af', 'aresample=async=1:first_pts=0',
         '-c:a', 'aac', '-b:a', '160k', '-ar', '48000', '-ac', '2', '-map_metadata', '-1', '-movflags', '+faststart', str(target)], WORK / 'concat.log')
    (WORK / '说明字幕.srt').write_text('\n'.join(subtitles), encoding='utf-8-sig')
    (WORK / '剪辑时间线.json').write_text(json.dumps({'sources':[str(PHONE),str(BACKEND)],'duration':cursor,'stages':timeline},ensure_ascii=False,indent=2),encoding='utf-8')
    card('intro').save(WORK / '演示封面.png')
    print(f'FINISHED {target} | {cursor:.2f} seconds', flush=True)


if __name__ == '__main__':
    main()
