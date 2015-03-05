var vent = require('vent');
var Backgrid = require('backgrid');

module.exports = Backgrid.Cell.extend({
    className : 'file-link-cell',

    render : function() {
        // Network accessible path to /mnt directory
        var replace = /^\/mnt\//;
        var replaceWith = "smb://192.168.1.1/main/";
        var externalLink = this.model.get('path').replace(replace, replaceWith);

        this.$el.empty();
        this.$el.html('<a><i class="icon-open-file x-open-file" title="Open"></a>');
        this.$("a").prop("href", externalLink);

        return this;
    },
});
