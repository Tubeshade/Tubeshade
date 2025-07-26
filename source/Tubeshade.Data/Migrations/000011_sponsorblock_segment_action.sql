ALTER TYPE media.sponsorblock_segment_category ADD VALUE IF NOT EXISTS 'filler';

UPDATE media.sponsorblock_segments
SET category = 'filler'
WHERE category = 'filter';

ALTER TABLE media.sponsorblock_segments
    ALTER category TYPE text;

DROP TYPE media.sponsorblock_segment_category;
CREATE TYPE media.sponsorblock_segment_category AS ENUM ('sponsor', 'selfpromo', 'interaction', 'intro', 'outro', 'preview', 'music_offtopic', 'filler');

ALTER TABLE media.sponsorblock_segments
    ALTER category TYPE media.sponsorblock_segment_category USING category::media.sponsorblock_segment_category;
