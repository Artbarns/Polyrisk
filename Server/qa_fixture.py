#!/usr/bin/env python3
"""Install or capture game fixtures on an explicitly selected loopback QA API."""
import argparse
import json
import pathlib
import urllib.parse
import urllib.request


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', required=True)
    parser.add_argument('--session-file', required=True, type=pathlib.Path)
    parser.add_argument('--fixture', type=pathlib.Path)
    parser.add_argument('--capture', type=pathlib.Path)
    args = parser.parse_args()
    origin = urllib.parse.urlsplit(args.base)
    if (origin.scheme != 'http' or origin.hostname not in ('127.0.0.1', 'localhost')
            or origin.username or origin.password or origin.query or origin.fragment
            or origin.path not in ('', '/')):
        parser.error('Only a loopback HTTP QA origin is allowed')
    if not args.fixture and not args.capture:
        parser.error('Specify --fixture or --capture')
    session = json.loads(args.session_file.read_text())
    code = session['lobby']['code']
    credentials = {key: session[key] for key in ('playerId', 'reconnectToken')}

    def post(path, values):
        request = urllib.request.Request(
            args.base.rstrip('/') + '/api/lobbies/' + code + path,
            data=json.dumps(credentials | values).encode(),
            headers={'Content-Type': 'application/json'}, method='POST')
        with urllib.request.urlopen(request, timeout=15) as response:
            return json.load(response)

    state = post('/game/state/get', {})
    if args.fixture:
        payload = json.loads(args.fixture.read_text())
        if 'snapshot' in payload:
            payload = payload['snapshot']
        lobby = post('/snapshot', {})
        if len(payload['players']) != len(lobby['players']):
            parser.error('Fixture seat count must match the selected lobby')
        for player in payload['players']:
            seat = player['playerId']
            if not isinstance(seat, int) or not 0 <= seat < len(lobby['players']):
                parser.error('Invalid fixture seat')
            player['lobbyPlayerId'] = lobby['players'][seat]['playerId']
            player['displayName'] = lobby['players'][seat]['displayName']
        state = post('/game/state', {
            'expectedStateVersion': state['version'],
            'lastAppliedCommandSequence': state['lastAppliedCommandSequence'],
            'payloadJson': json.dumps(payload),
        })
        print('Installed fixture:', code, 'version', state['version'])
    if args.capture:
        state = post('/game/state/get', {})
        payload = json.loads(state['payloadJson'])
        # Export gameplay evidence, never credentials or private command queue.
        for player in payload.get('players', []):
            player.pop('lobbyPlayerId', None)
        evidence = {'code': code, 'version': state['version'],
                    'sequence': state['lastAppliedCommandSequence'], 'snapshot': payload}
        args.capture.write_text(json.dumps(evidence, indent=2) + '\n')
        print('Captured game state:', code, 'version', state['version'])


if __name__ == '__main__':
    main()
