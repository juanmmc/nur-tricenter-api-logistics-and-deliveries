module.exports = {
    extends: ['@commitlint/config-conventional'],
    rules: {
        'type-enum': [
            2,
            'always',
            [
                'feat',
                'fix',
                'chore',
                'docs',
                'refactor',
                'test',
                'ci',
                'perf',
                'build'
            ]
        ],
        'header-max-length': [2, 'always', 100]
    },
};