const colors = require('tailwindcss/colors')

module.exports = {
    purge: {
        enabled: true,
        content: [
            './src/*.jsx',
            './src/**/*.jsx'
        ]
    },
    theme: {
        colors: {
            transparent: 'transparent',
            current: 'currentColor',
            black: colors.black,
            blue: colors.blue,
            white: colors.white,
            gray: colors.trueGray,
            indigo: colors.indigo,
            red: colors.rose,
            yellow: colors.amber
        }
    },
    variants: {extend: {backgroundColor: ['active']}},
    plugins: [],
}
