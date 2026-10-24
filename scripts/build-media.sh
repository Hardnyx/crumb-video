#!/usr/bin/env bash
# Cross-compile Windows x64 FFmpeg + x264 on Ubuntu. No opaque prebuilt media bundle.
set -euo pipefail
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$script_dir/media-versions.env"
output_dir="$(realpath -m "${1:-$script_dir/../artifacts/media}")"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT
prefix="$work_dir/prefix"
mkdir -p "$output_dir/tools" "$output_dir/sources" "$output_dir/licenses/media" "$output_dir/build-logs" "$prefix"
build_log="$output_dir/build-logs/media.log"
trap 'tail -80 "$build_log" >&2' ERR

download_source() {
    local repository="$1" revision="$2" checksum="$3" name="$4"
    curl --fail --location --retry 3 --connect-timeout 20 --max-time 300 \
        "https://codeload.github.com/$repository/tar.gz/$revision" -o "$output_dir/sources/$name.tar.gz"
    printf '%s  %s\n' "$checksum" "$output_dir/sources/$name.tar.gz" | sha256sum --check
    mkdir -p "$work_dir/$name"
    tar --no-same-owner -xzf "$output_dir/sources/$name.tar.gz" --strip-components=1 -C "$work_dir/$name"
}

download_source mirror/x264 "$X264_COMMIT" "$X264_SHA256" x264
download_source FFmpeg/FFmpeg "$FFMPEG_COMMIT" "$FFMPEG_SHA256" ffmpeg

export PKG_CONFIG_LIBDIR="$prefix/lib/pkgconfig"
export PKG_CONFIG_PATH="$PKG_CONFIG_LIBDIR"
(
    cd "$work_dir/x264"
    ./configure --prefix="$prefix" --host=x86_64-w64-mingw32 --cross-prefix=x86_64-w64-mingw32- \
        --enable-static --disable-cli --disable-opencl --bit-depth=8
    make -j"$(nproc)"
    make install
    cp COPYING "$output_dir/licenses/media/x264-COPYING.txt"
    cd "$work_dir/ffmpeg"
    ./configure --prefix="$prefix" --target-os=mingw32 --arch=x86_64 \
        --cross-prefix=x86_64-w64-mingw32- --enable-cross-compile --pkg-config=pkg-config \
        --disable-autodetect --enable-gpl --enable-version3 --enable-libx264 \
        --enable-static --disable-shared --disable-debug --disable-doc --disable-ffplay \
        --disable-network --disable-hwaccels --disable-devices --enable-indev=lavfi \
        --disable-pthreads --enable-w32threads \
        --extra-cflags="-I$prefix/include" --extra-ldflags="-L$prefix/lib -static"
    make -j"$(nproc)"
    cp ffmpeg.exe ffprobe.exe "$output_dir/tools/"
    cp COPYING.GPLv3 COPYING.LGPLv2.1 COPYING.LGPLv3 LICENSE.md "$output_dir/licenses/media/"
    cp ffbuild/config.log "$output_dir/build-logs/ffmpeg-config.log"
    x86_64-w64-mingw32-objdump -p ffmpeg.exe | sed -n 's/.*DLL Name: /DLL: /p'
) > "$build_log" 2>&1

cp "$script_dir/build-media.sh" "$script_dir/media-versions.env" "$output_dir/sources/"
cat > "$output_dir/sources/BUILDING.txt" <<'EOF'
These are the exact unmodified source archives used for the bundled media tools.
On Ubuntu 24.04 install build-essential, mingw-w64, nasm, pkg-config, curl, and xz-utils.
Place build-media.sh and media-versions.env in the same directory, then run:
  bash build-media.sh /absolute/path/to/output
The configure commands and compiler output are retained in build-logs/media.log.
Crumb starts ffmpeg and ffprobe as separate processes. These executables are GPLv3;
the MIT license for Crumb does not replace the licenses of these tools.
EOF
(cd "$output_dir" && sha256sum tools/*.exe sources/*.tar.gz > SHA256SUMS.txt)
echo "Windows media tools and corresponding sources are ready in $output_dir"
