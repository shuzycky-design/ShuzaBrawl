#!/bin/bash
# Сервер ShuzaBrawl. Использование:
#   ./start.sh          - запустить в текущем терминале (консоль команд сервера доступна)
#   ./start.sh screen   - запустить в фоне, в screen-сессии "ShuzaBrawl" (войти: screen -r ShuzaBrawl)
#   ./start.sh build    - собрать новую версию в run_stage (работающий сервер не затрагивается)
#   ./start.sh restart  - остановить сервер, применить сборку из run_stage и запустить снова
#                         (все игроки будут отключены на несколько секунд)
set -e
cd "$(dirname "$(readlink -f "$0")")"
export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1

start_db() {
    # База данных (MySQL в Docker, слушает только 127.0.0.1:3306)
    docker start shuzabrawl-mysql >/dev/null
    until docker exec shuzabrawl-mysql mysqladmin ping -uroot -p"$(python3 -c "import json;print(json.load(open('run/config.json'))['database_password'])")" --silent 2>/dev/null; do sleep 1; done
}

stop_server() {
    screen -S ShuzaBrawl -X quit >/dev/null 2>&1 || true
    for _ in $(seq 1 10); do pgrep -f "^dotnet run/IndusBrawl" >/dev/null || return 0; sleep 1; done
    pgrep -f "^dotnet run/IndusBrawl" | xargs -r kill -KILL
}

case "$1" in
build)
    rm -rf run_stage
    dotnet publish IndusBrawl.Laser.Server -c Release -o run_stage
    echo "Сборка готова в run_stage. Применить: ./start.sh restart"
    ;;
restart)
    stop_server
    if [ -d run_stage ]; then
        # config.json и данные сервера (json-файлы состояния) в run не перезаписываем
        rm -f run_stage/config.json
        cp -a run_stage/. run/
        rm -rf run_stage
    fi
    start_db
    mkdir -p logs; screen -L -Logfile logs/server.log -dmS ShuzaBrawl dotnet run/IndusBrawl.Laser.Server.dll
    echo "Сервер запущен в screen: screen -r ShuzaBrawl"
    ;;
screen)
    start_db
    mkdir -p logs; screen -L -Logfile logs/server.log -dmS ShuzaBrawl dotnet run/IndusBrawl.Laser.Server.dll
    echo "Сервер запущен в screen: screen -r ShuzaBrawl"
    ;;
*)
    start_db
    exec dotnet run/IndusBrawl.Laser.Server.dll
    ;;
esac
