ALTER TABLE media.videos
    DROP COLUMN searchable_index_value;

ALTER TABLE media.videos
    ADD COLUMN searchable_index_value tsvector NOT NULL GENERATED ALWAYS AS (
        setweight(to_tsvector('english', videos.name), 'A') ||
        setweight(to_tsvector('english', text_array_to_string(videos.tags, ' ')), 'B') ||
        setweight(to_tsvector('english', videos.description), 'C')
        ) STORED;

CREATE INDEX ON media.videos USING GIN (searchable_index_value);

ANALYSE;
