ALTER TABLE media.videos
    ALTER COLUMN searchable_index_value SET EXPRESSION AS (
        setweight(to_tsvector('english', videos.name), 'A') ||
        setweight(to_tsvector('english', text_array_to_string(videos.tags, ' ')), 'B') ||
        setweight(to_tsvector('english', videos.description), 'C'));
