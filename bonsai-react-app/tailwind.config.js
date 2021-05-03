const colors = require('tailwindcss/colors')

module.exports = {
    purge: {
        enabled: false,
        content: [
            './src/*.jsx',
            './src/**/*.jsx',
            './src/*.js',
            './src/**/*.js',
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
            green: colors.green,
            yellow: colors.amber,
            "bonsai-orange": {
                DEFAULT: "#ff8400"
            },
            "bonsai-green" : {
                500: "",
                DEFAULT: "#209756"
            },
            "bonsai-brown" : {
                DEFAULT: "#c75f2b"
            },
            "bonsai-pink" : {
                DEFAULT: "#da4478"
            },
            "bonsai-light-purple" : {
                DEFAULT: "#7f3c53"
            },
            "bonsai-dark-purple" : {
                DEFAULT: "#6f3d4f"
            },
            "bonsai-violet" : {
                DEFAULT: "#534d6e"
            },
            "bonsai-light-neutral" : {
                DEFAULT: "#fdeec8"
            },
            "bonsai-dark-neutral" : {
                DEFAULT: "#e7e7ba"
            },
        }
    },
    variants: {extend: {backgroundColor: ['active']}},
    plugins: [],
}
