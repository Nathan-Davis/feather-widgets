﻿; (function ($) {
    'use strict';

    /*
        Widget
    */
    var CommentsCountWidget = function (rootUrl, resources, useReviews) {
        if (rootUrl === null || rootUrl.length === 0)
            rootUrl = '/';
        else if (rootUrl.charAt(rootUrl.length - 1) !== '/')
            rootUrl = rootUrl + '/';

        this.rootUrl = rootUrl;
        this.resources = resources;
        this.useReviews = useReviews;
    };

    CommentsCountWidget.prototype = {
        getCommentsCounts: function () {
            var threadKeys = this.collectThreadIds();
            var commentsCountSubpath = this.useReviews ? 'reviews_statistics' : 'count';
            var getCommentsCountsUrl = this.rootUrl + 'comments/' + commentsCountSubpath + '?ThreadKey=' + encodeURIComponent(threadKeys);

            return $.ajax({
                type: 'GET',
                url: getCommentsCountsUrl,
                contentType: 'application/json',
                cache: false,
                accepts: {
                    text: 'application/json'
                },
                processData: false
            });
        },

        collectThreadIds: function () {
            var commmentsCounterControls = $('[data-sf-role="comments-count-wrapper"]');
            var uniqueKeys = {};
            for (var i = 0; i < commmentsCounterControls.length; i++) {
                uniqueKeys[$(commmentsCounterControls[i]).attr('data-sf-thread-key')] = true;
            }

            var threadKeys = [];
            $.each(uniqueKeys, function (key) {
                threadKeys.push(key);
            });

            return threadKeys;
        },

        setCommentsCounts: function (threadCountList) {
            var self = this;
            for (var i = 0; i < threadCountList.length; i++) {
                if (threadCountList[i].Count == -1) {
                    continue;
                }

                $('div[data-sf-thread-key="' + threadCountList[i].Key + '"]').each(self.populateCommentsCountTextCallBack(threadCountList[i].Count, threadCountList[i].AverageRating));
            }
        },

        populateCommentsCountTextCallBack: function (currentCount, currentRating) {
            var self = this;
            return function (index, element) {
                self.populateCommentsCountText($(element), currentCount, currentRating);
            };
        },

        populateCommentsCountText: function (element, currentCount, currentRating) {
            var currentCountFormatted = '';
            if (!currentCount) {
                currentCountFormatted = this.resources.leaveComment;
            }
            else {
                currentCountFormatted = currentCount;

                if (currentCount == 1)
                    currentCountFormatted += ' ' + this.resources.comment.toLowerCase();
                else
                    currentCountFormatted += ' ' + this.resources.commentsPlural.toLowerCase();
            }

            //set the comments count text in the counter control
            var countAnchorText = element.find('[data-sf-role="comments-count-anchor-text"]');
            countAnchorText.text(currentCountFormatted);

            if (currentCount && this.useReviews) {
                var ratingEl = $('<span />');
                ratingEl.mvcRating({ readOnly: true, value: currentRating });
                ratingEl.prepend($('<span />').text(this.resources.averageRating));
                ratingEl.append($('<span />').text('(' + currentRating + ')'));

                element.prepend(ratingEl);
            }
        },

        initialize: function () {
            var self = this;

            self.getCommentsCounts().then(function (response) {
                if (response) {
                    self.setCommentsCounts(response.Items || response);
                }
            });

            $(document).on('sf-comments-count-received', function (event, args) {
                $('div[data-sf-thread-key="' + args.key + '"]').each(self.populateCommentsCountTextCallBack(args.count));
            });
        }
    };

    /*
        Widgets creation
    */
    $(function () {
        var serviceUrl = $('[data-sf-role="comments-count-wrapper"]').find('[data-sf-role="service-url"]').val();
        var useReviews = Boolean($('[data-sf-role="comments-count-wrapper"]').find('[data-sf-role="comments-use-reviews"]').val());
        var resources = JSON.parse($('[data-sf-role="comments-count-wrapper"]').find('[data-sf-role="comments-count-resources"]').val());
        (new CommentsCountWidget(serviceUrl, resources, useReviews)).initialize();
    });
}(jQuery));
