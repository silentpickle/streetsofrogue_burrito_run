"""Independent inventory/menu/native-progression checks for the disposable banana encounter."""
CASES = {'cash', 'kit', 'missing-banana', 'full-reward', 'full-recipient',
         'one-recipient-lost', 'both-recipients-lost', 'source-lost', 'abandon', 'pause', 'level-change'}
EXCHANGES = {'cash': 0, 'kit': 1, 'one-recipient-lost': 1, 'missing-banana': 0,
             'full-reward': 1, 'full-recipient': 0}


def mapping(value):
    return value if isinstance(value, dict) else {}


def progression(value):
    d = mapping(value)
    return (isinstance(d.get('quests'), list) and bool(d['quests']) and type(d.get('numQuests')) is int and
            type(d.get('floorComplete')) is bool and type(d.get('canExit')) is bool)


def assess(report):
    rows = [mapping(r.get('data')) for r in report.get('native_tests', []) if r.get('event') == 'native-test-quest-case']
    checks = []

    def check(name, value):
        checks.append(dict(name=name, passed=bool(value)))

    def one(name, phase):
        values = [r for r in rows if r.get('case') == name and r.get('phase') == phase]
        return values[0] if len(values) == 1 else {}

    startup = [mapping(r.get('data')) for r in report.get('native_tests', []) if r.get('event') == 'native-test-quest-startup']
    for phase in ('offered', 'settled'):
        values = [r for r in startup if r.get('phase') == phase]
        data = values[0] if len(values) == 1 else {}
        check('Normal startup ' + phase + ': favor exists on playable floor after character selection',
              data.get('state') == 'Offered' and data.get('sameEncounter') is True and data.get('ready') is True and
              data.get('characterSelect') is False and type(data.get('level')) is int and data['level'] > 0 and
              type(data.get('nativeQuestCount')) is int and data['nativeQuestCount'] > 0 and bool(data.get('floor')))

    check('No quest failure records', not any(r.get('phase') == 'failed' for r in rows))
    complete = [r for r in rows if r.get('phase') == 'complete']
    check('Ten pre-transition cases completed once', len(complete) == 10 and {r.get('case') for r in complete} == CASES - {'level-change'})
    for name in sorted(CASES):
        placement = mapping(one(name, 'placement').get('extra'))
        check(name + ': ordinary production placement has two actors, one safe unprotected banana',
              all(placement.get(k) is True for k in ('buyer', 'neighbor', 'source', 'ordinaryLevel', 'unprotected', 'buyerApproach', 'neighborApproach', 'bananaApproach')))
        menus = one(name, 'menus')
        data = mapping(menus.get('extra'))
        check(name + ': native menus expose both alternatives after acceptance', menus.get('state') == 'Accepted' and
              'BurritoRun_FavorAccept' in (data.get('before') or []) and
              'BurritoRun_FavorCash' in (data.get('buyer') or []) and 'BurritoRun_FavorKit' in (data.get('neighbor') or []))
        check(name + ': acceptance preserves native quest and exit state', progression(data.get('nativeBefore')) and
              data.get('nativeBefore') == data.get('nativeAfter'))
    for name, recipient in EXCHANGES.items():
        row = one(name, 'exchange')
        data = mapping(row.get('extra'))
        before, after = mapping(data.get('before')), mapping(data.get('after'))
        fields = ('playerBananas', 'recipientBananas', 'money', 'kits', 'playerHealth')
        valid = (all(type(before.get(k)) is int and type(after.get(k)) is int for k in fields[:4]) and
                 type(before.get('playerHealth')) in (int, float) and type(after.get('playerHealth')) in (int, float))
        check(name + ': native quest and exit state unchanged by exchange', progression(data.get('nativeBefore')) and
              data.get('nativeBefore') == data.get('nativeAfter'))
        if name in ('cash', 'kit', 'one-recipient-lost'):
            check(name + ': exactly one banana and selected reward verified', valid and data.get('recipient') == recipient and
                  row.get('state') == 'Completed' and row.get('winner') == ('buyer' if recipient == 0 else 'neighbor') and
                  after['playerBananas'] == before['playerBananas'] - 1 and after['recipientBananas'] == before['recipientBananas'] + 1 and
                  after['money'] == before['money'] + (15 if recipient == 0 else 0) and
                  after['kits'] == before['kits'] + (1 if recipient == 1 else 0) and after['playerHealth'] == before['playerHealth'])
            check(name + ': native pickup retired ground item', data.get('pickupVerified') is True)
            check(name + ': actual native thanks bubble', data.get('thanksBubble') is True)
            check(name + ': repeat and other recipient calls cannot pay again', bool(data.get('duplicateBefore')) and
                  data.get('duplicateBefore') == data.get('duplicateAfter'))
            pickup = mapping(one(name, 'pickup').get('extra'))
            check(name + ': native Item.Interact gained exactly one banana', type(pickup.get('before')) is int and
                  type(pickup.get('after')) is int and pickup['after'] == pickup['before'] + 1 and pickup.get('groundActive') is False)
        else:
            check(name + ': refusal preserves inventory and health', valid and row.get('state') == 'Accepted' and row.get('winner') is None and
                  all(before[k] == after[k] for k in fields))
    for name, state in (('both-recipients-lost', 'Unavailable'), ('source-lost', 'Unavailable'), ('abandon', 'Abandoned'), ('pause', 'Accepted')):
        check(name + ': correct terminal or paused state', one(name, 'complete').get('state') == state)
    pause = mapping(one('pause', 'pause').get('extra'))
    check('Real pause preserves quest, inventory and game clock', pause.get('unchanged') is True and pause.get('timeScale') == 0)
    scope = mapping(one('level-change', 'scope-ended').get('extra'))
    check('Native reload clears encounter and expires unfinished favor', scope.get('cleared') is True and scope.get('state') == 'Expired' and
          type(scope.get('oldLevel')) is int and type(scope.get('newLevel')) is int and scope['newLevel'] > scope['oldLevel'])
    return dict(checks=checks)
