ALTER TABLE media.sponsorblock_segments
    ALTER category TYPE text;

UPDATE media.sponsorblock_segments
SET category = 'filler'
WHERE category = 'filter';

DROP TYPE media.sponsorblock_segment_category;
CREATE TYPE media.sponsorblock_segment_category AS ENUM ('sponsor', 'selfpromo', 'interaction', 'intro', 'outro', 'preview', 'music_offtopic', 'filler');

ALTER TABLE media.sponsorblock_segments
    ALTER category TYPE media.sponsorblock_segment_category USING category::media.sponsorblock_segment_category;
