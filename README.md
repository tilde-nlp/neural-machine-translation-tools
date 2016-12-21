# Neural Machine Translation Tools 

Neural Machine Translation (NMT) Tools is a collection of tools for alignment extraction from NMT attention-based alignment matrices and smarter processing of unknown words in factored models.

This repository contains the following implementations:
* A [tool](ProcessNMTAlignments) for __Flat__ word alignment extraction extraction from NMT system alignment matrices. The tool implements the algorithms from the following paper (with some modifications that are described [here](https://github.com/tilde-nlp/et-mt-tools)):
    ```
    @inproceedings{Pinnis2016,
        address = {Riga, Latvia},
        author = {Pinnis, Mārcis},
        booktitle = {Human Language Technologies – The Baltic Perspective - Proceedings of the Seventh International Conference      Baltic HLT 2016},
        doi = {10.3233/978-1-61499-701-6-84},
        isbn = {9781614997016},
        keywords = {english,hybrid system,latvian,neural machine translation},
        pages = {84--91},
        publisher = {IOS Press},
        title = {{Towards Hybrid Neural Machine Translation for English-Latvian}},
        year = {2016}
    }
    ```
    The implementation allows to build NMT-SMT hybrid systems for out-of-vocabulary word handling. The tool allows creating Moses XML for NMT translations that can then be translated with a Moses SMT system (with disabled phrase re-ordering). This was initially intended for word-based NMT systems that have real issues with out-of-vocabulary word handling. When character-based NMT systems will take over (the world), this will become obsolete, however the byte-pair-encoding-based methods still produce out-of-vocabulary words and a hybrid scenario may be necessary (especially when dealing with named entities that are poorly translated with current NMT systems).
* A [tool](AttentionMatrixToAlignment) for __Flat__ word alignment extraction from NMT system alignment matrices that can be acquired using the [tilde-nlp](https://github.com/tilde-nlp) fork of the [amunmt decoder](https://github.com/tilde-nlp/amunmt). The __flat__ word alignments from this implementation can be used to integrate the NMT systems in existing document translation workflows (e.g., SMT document translation workflows) where formatting tag processing is crucial. This tool relies on the word alignment extraction algorithms of the first tool.
* A [tool](ReplaceRareWordsWithPOSTags) that allows to create non-factored NMT system training data by replacing rare words with their part-of-speech (or morpho-syntactic) tags. The input has to be prepared in a [Moses](http://www.statmt.org/moses/?n=FactoredTraining.PrepareTraining) factored data format where the tag is always the last factor.